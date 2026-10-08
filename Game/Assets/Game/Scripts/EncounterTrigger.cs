using UnityEngine;

namespace SwordPrototype
{
    public sealed class EncounterTrigger : MonoBehaviour
    {
        public bool Started { get; private set; }
        private void OnTriggerEnter(Collider other)
        {
            if (!Started && other.GetComponent<PlayerMovement>() != null)
            {
                Started = true;
                Debug.Log("Arena entered: encounter started. Enemy combat awaits approved rules.");
            }
        }
    }
}
