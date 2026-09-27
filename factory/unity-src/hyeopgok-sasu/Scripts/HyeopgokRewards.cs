using System;
using UnityEngine;

namespace Mgf.HyeopgokSasu
{
    public enum HyeopgokTowerKind { Crossbow=0, Cannon=1, Magic=2 }
    public enum HyeopgokRewardKind { None=0, Coin=1, Tower=2 }

    [Serializable]
    public struct HyeopgokChestReward
    {
        public HyeopgokRewardKind kind;
        public HyeopgokTowerKind towerType;
        public int coinBonus, towerSlot;
        public string KindName=>kind==HyeopgokRewardKind.Coin?"coin":kind==HyeopgokRewardKind.Tower?"tower":"none";
        public string TowerName=>towerType==HyeopgokTowerKind.Cannon?"cannon":towerType==HyeopgokTowerKind.Magic?"magic":"crossbow";
    }

    // Reward entropy is intentionally isolated from question order, choice placement
    // and adjudication. A wrong answer returns before consuming even one random draw.
    public sealed class HyeopgokRewardRng
    {
        uint state;
        public int DrawCount { get; private set; }
        public HyeopgokRewardRng(int seed){Reset(seed);}
        public void Reset(int seed){state=(uint)seed^0xa341316cu;if(state==0)state=0x9e3779b9u;DrawCount=0;}
        uint Next(){state^=state<<13;state^=state>>17;state^=state<<5;DrawCount++;return state;}
        float Unit()=>((Next()>>8)&0x00ffffffu)/16777216f;
        public HyeopgokChestReward Roll(bool correct,int streak,bool hasEmptyPlot,float speed01,bool forceCoin=false,bool forceTower=false)
        {
            if(!correct)return new HyeopgokChestReward{kind=HyeopgokRewardKind.None,towerSlot=-1};
            float chance=streak>=3?1f:.30f+.05f*Mathf.Clamp(streak-1,0,1);
            bool tower=hasEmptyPlot&&(forceTower||(!forceCoin&&Unit()<chance));
            if(tower)return new HyeopgokChestReward{
                kind=HyeopgokRewardKind.Tower,
                towerType=(HyeopgokTowerKind)(Next()%3),towerSlot=-1
            };
            return new HyeopgokChestReward{
                kind=HyeopgokRewardKind.Coin,
                coinBonus=20+Mathf.RoundToInt(20*Mathf.Clamp01(speed01)),towerSlot=-1
            };
        }
    }
}
