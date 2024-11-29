using System;
using UnityEngine;

namespace Game.Objectives
{
    [CreateAssetMenu(fileName = "New Dialog", menuName = "Game/Dialogs", order = 10)]
    internal class AgentDialog : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private DialogEntry[] _entries;
        public string Name { get => _name; }
        public DialogEntry[] Entries => _entries;
    }

    [Serializable]
    public class DialogEntry
    {
        [SerializeField][TextArea] private string _content;
        [SerializeField] private bool _useBranch;
        [SerializeField] private DialogEntryBranch _branch;
        [SerializeField][Range(3, 10)] private int _duration;

        public string Content { get => _content; }
        public int Duration { get => _duration; }
        public bool UseBranch { get => _useBranch; }
        public DialogEntryBranch Branch { get => _branch; }
    }

    [Serializable]
    public class DialogEntryBranch
    {
        [SerializeField][TextArea] private string _positiveContent;
        [SerializeField][TextArea] private string _negativeContent;
        [SerializeField][Range(3, 10)] private int _duration;

        public string PositiveContent { get => _positiveContent; }
        public string NegativeContent { get => _negativeContent; }
        public int Duration { get => _duration; }
    }
}