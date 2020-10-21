using JetBrains.Annotations;
using UnityEngine;

namespace UnityEngine.Recorder.Tests
{
    internal class RecordableMonoBehaviour : MonoBehaviour
    {
// Compiler yapping about unused members (they are initialized through serialization)
#pragma warning disable 0649
        public enum SomeEnum
        {
            Value1 = 0,
            Value2 = 1,
            Value3 = 2
        };


        public bool boolMember;
        public SomeEnum enumMember;
        public int intMember;
        public Vector3 vectMember;
        public Quaternion quatMember;
#pragma warning restore 0649
    }
}
