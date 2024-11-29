using Core.Engine;
using Game.Objectives;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public class HUDObjective : MonoBehaviour
    {
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _desc;

        private ObjectiveService _objectives;

        private IEnumerator Start()
        {
            _objectives = Bootstrap.Resolve<ObjectiveService>();
            _objectives.ReckonerChanged += OnReckonerChange;
            _objectives.AdvanceEvent += OnAdvance;

            yield return new WaitForEndOfFrame();

            _name.text = _objectives.GetCurrentObjective().TaskName;
            _desc.text = _objectives.GetCurrentObjective().TaskDescription;
        }

        private void OnReckonerChange(ObjectiveReckoner reckoner, Objective current)
        {
            _name.text = current.TaskName;
            _desc.text = current.TaskDescription;
        }

        private void OnAdvance(ObjectiveReckoner reckoner, Objective current)
        {
            _name.text = current.TaskName;
            _desc.text = current.TaskDescription;
        }
    }
}