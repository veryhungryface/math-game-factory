using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.MuneoSajinsa
{
    public partial class MuneoSajinsaGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int round;
            public int remainingTime;
            public int firstAttemptCorrect;
            public int firstAttemptTotal;
            public int shotsUsed;
            public int attemptIndex;
            public int photoCount;
            public int pointerVersion;
            public bool onboarding;
            public string problemId = "";
            public string currentProps = "";
            public string photoIds = "";
            public string lastFailureReason = "";
            public string misconceptionId = "";
        }

        enum GamePhase { Title, Practice, Playing, Feedback, End }
        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly List<string> photos = new List<string>(MuneoRules.ShotLimit);
        readonly bool[] firstAttemptByRound = new bool[MuneoRules.Goal];
        GamePhase phase = GamePhase.Title;
        PhotoTask current;
        string wornLeft = "", wornRight = "";
        int runSeed;
        bool attempted;
        float remainingTime;
        float feedbackClock;
        float idleGuide;
        bool feedbackCorrect;
        bool feedbackPractice;
        bool dragging;
        int dragGroup = -1, dragIndex = -1, dragPhoto = -1;
        Vector2 dragStart;
        bool dragMoved;
        bool tearing;

        void Awake()
        {
            MgfLook.Quality(34f);
            bank.AddRange(MuneoRules.BuildBank());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("문어사진사소품을입혀모든모습을찍어라렌즈를눌러사진관열기중학교2학년경우의수최고첫제출보호고리촬영필름절취선휴지통");
            sb.Append("모자깃발고깔둥근챙조개별산호물결점줄달십의자리일의자리두자리자연수같은카드는두번쓰지않는다");
            sb.Append("모든모습을한번씩찍으면사진벽이열린다소품을끌어복어에게입히시오렌즈를눌러촬영하시오필름끝을위로당겨제출하시오");
            sb.Append("정확합니다아직찍지않은모습이있습니다중복되었습니다조건에맞지않습니다연습완료다시촬영결과사진벽완성");
            sb.Append("압력선체암실가능한장면을전부현상하라황동안전등현상격자카드를누르거나착용자리로끌어놓으시오실수여유목표");
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandlePointer();
            if (phase == GamePhase.Playing)
            {
                remainingTime -= dt;
                st.remainingTime = Mathf.Max(0, Mathf.CeilToInt(remainingTime));
                idleGuide += dt;
                if (remainingTime <= 0f) EndRun(false, "시간이 끝났습니다.");
                else if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(); }
            }
            else if (phase == GamePhase.Practice)
            {
                idleGuide += dt;
                if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(); }
            }
            else if (phase == GamePhase.Feedback)
            {
                feedbackClock += dt;
                float feedbackDuration = feedbackPractice && feedbackCorrect ? .9f : (feedbackCorrect ? 2.0f : 1.65f);
                if (feedbackClock >= feedbackDuration) FinishFeedback();
            }
            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            phase = GamePhase.Title; current = MuneoRules.Practice();
            st.score = 0; st.lives = MuneoRules.StartLives; st.level = 1; st.solved = 0; st.round = 0;
            st.remainingTime = MuneoRules.RunSeconds; st.firstAttemptCorrect = 0; st.firstAttemptTotal = 0;
            st.shotsUsed = 0; st.attemptIndex = 0; st.photoCount = 0; st.pointerVersion = 0;
            st.onboarding = false; st.phase = "title"; st.problemId = current.id; st.currentProps = ""; st.photoIds = "";
            st.lastFailureReason = ""; st.misconceptionId = "";
            photos.Clear(); wornLeft = wornRight = ""; dragging = tearing = false;
            ResetWorldForRun(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void BeginRun(bool onboarding)
        {
            runSeed++;
            Array.Clear(firstAttemptByRound, 0, firstAttemptByRound.Length);
            phase = onboarding ? GamePhase.Practice : GamePhase.Playing;
            st.score = 0; st.lives = MuneoRules.StartLives; st.level = 1; st.solved = 0; st.round = 0;
            st.remainingTime = MuneoRules.RunSeconds; st.firstAttemptCorrect = 0; st.firstAttemptTotal = 0;
            st.attemptIndex = 0; st.pointerVersion = 0; st.onboarding = onboarding; st.phase = "playing";
            st.lastFailureReason = ""; st.misconceptionId = ""; remainingTime = MuneoRules.RunSeconds;
            ResetWorldForRun();
            if (onboarding) LoadPractice(); else LoadRound();
            SetScreen(); OpenShutter(); MgfSfx.Play("whoosh", .28f); MgfBridge.NotifyChanged();
        }

        void LoadPractice()
        {
            current = MuneoRules.Practice(); attempted = false; st.problemId = current.id; st.level = 1;
            st.attemptIndex = 0; ResetAttempt(); RefreshProblemUi(); StartGuide();
        }

        void LoadRound()
        {
            current = MuneoRules.SessionTask(st.solved, runSeed); attempted = false;
            st.round = st.solved + 1; st.level = current.band; st.problemId = current.id; st.attemptIndex = 0;
            ResetAttempt(); RefreshProblemUi();
        }

        void ResetAttempt()
        {
            photos.Clear(); wornLeft = wornRight = ""; st.shotsUsed = 0; st.photoCount = 0;
            st.currentProps = ""; st.photoIds = ""; st.lastFailureReason = ""; st.misconceptionId = "";
            dragging = tearing = false; dragPhoto = -1; idleGuide = 0f;
            ResetWorldForProblem(); RebuildPropCards(); RefreshFilm(); RefreshHud(); MgfBridge.NotifyChanged();
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++; idleGuide = 0f; SpawnRipple(MgfPointer.Position); MgfBridge.NotifyChanged();
                if (phase == GamePhase.Title)
                {
                    if (HitTitleLens(MgfPointer.Position)) BeginRun(true);
                    else { Refuse("중앙 카메라 렌즈를 누르시오."); PulseTitleLens(); }
                    return;
                }
                if (phase == GamePhase.End)
                {
                    if (HitRestart(MgfPointer.Position)) BeginRun(true);
                    else { Refuse("산호 집게를 눌러 새 필름을 여시오."); PulseRestart(); }
                    return;
                }
                if (phase == GamePhase.Feedback) { Refuse("필름을 현상하고 있습니다."); return; }
                if (phase != GamePhase.Practice && phase != GamePhase.Playing) return;

                int prop = HitProp(MgfPointer.Position, out int group);
                if (prop >= 0)
                {
                    dragging = true; dragGroup = group; dragIndex = prop; dragPhoto = -1; dragStart = MgfPointer.Position; dragMoved = false;
                    BeginDragGhost(group, prop, MgfPointer.Position); AnticipateProp(group, prop); MgfSfx.Play("tap", .12f); return;
                }
                int photo = HitPhoto(MgfPointer.Position);
                if (photo >= 0)
                {
                    dragging = true; dragPhoto = photo; dragGroup = -1; dragIndex = -1; dragStart = MgfPointer.Position; dragMoved = false;
                    BeginPhotoDrag(photo, MgfPointer.Position); return;
                }
                if (HitCamera(MgfPointer.Position)) { CapturePhoto(); return; }
                if (HitTear(MgfPointer.Position))
                {
                    tearing = true; dragging = false; dragStart = MgfPointer.Position; dragMoved = false; AnticipateTear(); return;
                }
                if (phase == GamePhase.Practice && photos.Count >= current.answerCount) { SubmitFilm(); return; }
                if (phase == GamePhase.Practice && (wornLeft.Length > 0 || wornRight.Length > 0)) { CapturePhoto(); return; }
                if (phase == GamePhase.Practice)
                {
                    WearProp(photos.Count == 0 ? 0 : 1, 0);
                    ShowToast("연습에서는 카드가 한 장뿐입니다. 화면을 한 번 더 눌러 촬영하시오.");
                    return;
                }
                Refuse(CurrentInstruction()); PointToNextAction();
            }

            if (MgfPointer.Held)
            {
                if (dragging)
                {
                    if ((MgfPointer.Position - dragStart).sqrMagnitude > 144f) dragMoved = true;
                    MoveDragGhost(MgfPointer.Position);
                }
                if (tearing)
                {
                    if (MgfPointer.Position.y - dragStart.y > 16f) dragMoved = true;
                    MoveTear(MgfPointer.Position.y - dragStart.y);
                }
            }

            if (MgfPointer.Up)
            {
                if (dragging)
                {
                    bool moved = dragMoved; dragging = false; EndDragGhost();
                    if (dragPhoto >= 0)
                    {
                        if (!moved) { Refuse("버릴 사진은 휴지통까지 끌어 놓으시오."); PointToTrash(); return; }
                        DropPhoto(MgfPointer.Position);
                    }
                    else if (!moved) WearProp(dragGroup, dragIndex);
                    else DropProp(MgfPointer.Position);
                }
                if (tearing)
                {
                    tearing = false; EndTear();
                    if (dragMoved && MgfPointer.Position.y - dragStart.y >= 64f) SubmitFilm();
                    else { Refuse("필름 끝을 절취선 위로 64만큼 당기시오."); PointToTear(); }
                }
            }
        }

        void DropProp(Vector2 screen)
        {
            int slot = HitWearSlot(screen);
            if (slot < 0) { Refuse("피사체의 표시된 착용 자리 가까이에 놓으시오."); PointDragPath(dragGroup, dragIndex); return; }
            if (slot != dragGroup)
            {
                Refuse(current.kind == TaskKind.NumberCards ? (dragGroup == 0 ? "이 카드는 십의 자리에 놓으시오." : "이 카드는 일의 자리에 놓으시오.") : (dragGroup == 0 ? "모자는 머리 자리에 놓으시오." : "깃발은 등 자리에 놓으시오."));
                PointDragPath(dragGroup, dragIndex); return;
            }
            WearProp(dragGroup, dragIndex);
        }

        void WearProp(int group, int index)
        {
            if (group < 0 || group > 1 || index < 0) return;
            string[] labels = group == 0 ? current.leftLabels : current.rightLabels;
            if (index >= labels.Length) return;
            string value = labels[index];
            if (group == 0) wornLeft = value; else wornRight = value;
            if (current.kind == TaskKind.Sum)
            {
                if (group == 0) wornRight = ""; else wornLeft = ""; // '또는': 동시에 착용할 수 없는 사건
            }
            st.currentProps = (wornLeft.Length == 0 ? "없음" : wornLeft) + "|" + (wornRight.Length == 0 ? "없음" : wornRight);
            ApplyWornVisual(); PunchPuffer(); RefreshHud(); MgfSfx.Play("pop", .18f); MgfBridge.NotifyChanged();
            if (phase == GamePhase.Practice) GuideCamera();
        }

        void DropPhoto(Vector2 screen)
        {
            if (HitTrash(screen) && dragPhoto >= 0 && dragPhoto < photos.Count)
            {
                string removed = photos[dragPhoto]; photos.RemoveAt(dragPhoto); st.photoCount = photos.Count; st.photoIds = string.Join(",", photos);
                RefreshFilm(); ShowToast(removed + " 사진을 버렸습니다. 촬영 횟수는 돌아오지 않습니다."); MgfSfx.Play("whoosh", .14f); MgfBridge.NotifyChanged();
            }
            else { Refuse("버릴 사진은 휴지통까지 끌어 놓으시오."); PointToTrash(); }
        }

        void CapturePhoto()
        {
            if (st.shotsUsed >= MuneoRules.ShotLimit) { Resolve(false, "촬영 14회를 모두 사용했습니다.", "shot-limit"); return; }
            string id = CurrentPhotoId();
            if (id == null) { Refuse(current.kind == TaskKind.Sum ? "모자 또는 깃발 중 하나를 먼저 놓으시오." : "두 착용 자리를 모두 채우시오."); BlinkLensClosed(); return; }
            st.shotsUsed++; photos.Add(id); st.photoCount = photos.Count; st.photoIds = string.Join(",", photos);
            ShootAnimation(id, photos.Count - 1); MgfSfx.Play("pop", .28f);
            wornLeft = wornRight = ""; st.currentProps = ""; ApplyWornVisual(); ShufflePropCards(); RefreshFilm(); RefreshHud(); MgfBridge.NotifyChanged();
            if (phase == GamePhase.Practice)
            {
                if (photos.Count < current.answerCount) { ShowToast("첫 사진입니다. 다른 소품 한 가지도 찍으시오."); PointToNextProp(); }
                else { ShowToast("연습 필름을 자동 현상합니다. 실전에서는 필름 끝을 당기시오."); SubmitFilm(); }
            }
        }

        string CurrentPhotoId()
        {
            if (current.kind == TaskKind.Sum)
            {
                if (wornLeft.Length > 0 && wornRight.Length == 0) return "H" + Array.IndexOf(current.leftLabels, wornLeft);
                if (wornRight.Length > 0 && wornLeft.Length == 0) return "F" + Array.IndexOf(current.rightLabels, wornRight);
                return null;
            }
            if (wornLeft.Length == 0 || wornRight.Length == 0) return null;
            if (current.kind == TaskKind.Product) return "H" + Array.IndexOf(current.leftLabels, wornLeft) + "|F" + Array.IndexOf(current.rightLabels, wornRight);
            return wornLeft + wornRight;
        }

        void SubmitFilm()
        {
            if (photos.Count == 0) { Refuse("사진을 찍은 뒤 필름을 당기시오."); PointToCamera(); return; }
            bool correct = MuneoRules.IsExact(current, photos, out string reason, out string misconception);
            Resolve(correct, reason, misconception);
        }

        void Resolve(bool correct, string reason, string misconception)
        {
            if (phase != GamePhase.Practice && phase != GamePhase.Playing) return;
            feedbackPractice = phase == GamePhase.Practice; feedbackCorrect = correct; feedbackClock = 0f;
            phase = GamePhase.Feedback; dragging = tearing = false; st.lastFailureReason = reason; st.misconceptionId = misconception;
            if (correct)
            {
                if (!feedbackPractice)
                {
                    if (!attempted) { st.firstAttemptTotal++; st.firstAttemptCorrect++; firstAttemptByRound[st.solved] = true; }
                    st.solved++; st.score += 100 + (MuneoRules.ShotLimit - st.shotsUsed) * 5;
                }
                st.phase = "playing"; PlayCorrectFilm(current.reveal, photos); MgfSfx.Play("correct", .38f);
            }
            else
            {
                if (!feedbackPractice)
                {
                    if (!attempted) st.firstAttemptTotal++;
                    attempted = true; st.lives--; st.attemptIndex++;
                }
                st.phase = st.lives <= 0 ? "gameover" : "playing";
                PlayWrongFilm(reason, misconception); MgfSfx.Play("wrong", .34f);
            }
            RefreshHud(); MgfBridge.NotifyChanged();
        }

        void FinishFeedback()
        {
            if (phase != GamePhase.Feedback) return;
            StopFeedbackVisuals();
            if (feedbackPractice)
            {
                if (feedbackCorrect)
                {
                    phase = GamePhase.Playing; st.onboarding = false; remainingTime = MuneoRules.RunSeconds; LoadRound(); SetScreen();
                    ShowToast("모자만 1가지 + 깃발만 1가지 = 2가지. 이제 실전입니다.");
                }
                else { phase = GamePhase.Practice; ResetAttempt(); StartGuide(); }
            }
            else if (feedbackCorrect)
            {
                if (st.solved >= MuneoRules.Goal)
                {
                    bool mastered = st.firstAttemptCorrect >= 5;
                    EndRun(mastered, mastered ? "현상 격자를 완성했습니다." : "완주했지만 첫 제출 목표 5/6에 도달하지 못했습니다.");
                }
                else { phase = GamePhase.Playing; LoadRound(); SetScreen(); }
            }
            else
            {
                if (st.lives <= 0) EndRun(false, "카메라 보호고리 3개가 모두 갈라졌습니다.");
                else { phase = GamePhase.Playing; ResetAttempt(); ShowToast("같은 의뢰를 다시 촬영하시오."); }
            }
            MgfBridge.NotifyChanged();
        }

        void EndRun(bool mastered, string reason)
        {
            phase = GamePhase.End; st.phase = mastered ? "clear" : (st.solved >= MuneoRules.Goal ? "practice_complete" : "gameover");
            st.onboarding = false; st.lastFailureReason = reason; ShowEnd(mastered, reason); MgfSfx.Play(mastered ? "win" : "lose", .42f); MgfBridge.NotifyChanged();
        }

        string CurrentInstruction()
        {
            if (wornLeft.Length == 0 && wornRight.Length == 0) return "소품 카드를 누르거나 착용 자리로 끌어 놓으시오.";
            if (current.kind != TaskKind.Sum && (wornLeft.Length == 0 || wornRight.Length == 0)) return "남은 착용 자리도 채우시오.";
            return "렌즈를 눌러 현재 모습을 촬영하시오.";
        }

        void ReadyForHook()
        {
            if (phase == GamePhase.Title || phase == GamePhase.End || phase == GamePhase.Practice) BeginRun(false);
            else if (phase == GamePhase.Feedback) { feedbackClock = 99f; FinishFeedback(); if (phase == GamePhase.End) BeginRun(false); }
        }

        public void TestStart() { BeginRun(false); }
        public void TestAnswerCorrect()
        {
            ReadyForHook(); photos.Clear(); photos.AddRange(current.validIds); st.shotsUsed = current.answerCount;
            st.photoCount = photos.Count; st.photoIds = string.Join(",", photos); RefreshFilm(); SubmitFilm();
        }
        public void TestAnswerWrong()
        {
            ReadyForHook(); photos.Clear(); photos.AddRange(MuneoRules.DeliberateWrong(current, out string misconception));
            st.shotsUsed = photos.Count; st.photoCount = photos.Count; st.photoIds = string.Join(",", photos); RefreshFilm();
            Resolve(false, misconception == "zero-in-tens" ? "0은 십의 자리에 놓을 수 없습니다." : "합과 곱의 조건을 다시 확인하시오.", misconception);
        }
        public string StateJson() => JsonUtility.ToJson(st);
        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
