using System;

namespace Mgf.HyeopgokSasu
{
    [Serializable]
    public sealed class PackIndex
    {
        public string default_pack;
        public PackEntry[] packs;
    }

    [Serializable]
    public sealed class PackEntry
    {
        public string pack_id, title, file;
    }

    [Serializable]
    public sealed class QuestionPack
    {
        public string pack_id, title, school, unit_id;
        public int grade, semester;
        public string[] standards;
        public PackItem[] items;
    }

    [Serializable]
    public sealed class PackItem
    {
        public string id, prompt, answer, format, explain, unitConcept;
        public string[] choices, distractor_tags;
        // QA display only. Gameplay compares the canonical answer token exactly.
        public double answerNumeric;
        public int difficulty;
    }
}
