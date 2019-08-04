using System.Collections.Generic;

namespace Enemy
{
    public static class AiGraphClient
    {
#if UNITY_EDITOR
        private static List<Machine> machines = new List<Machine>();
#endif

        public static void RegisterMachine(string name, AiStateMachine machine)
        {
#if UNITY_EDITOR
            machines.Add(new Machine {name = name, machine = machine});
#endif
        }

#if UNITY_EDITOR
        public static List<Machine> GetMachines()
            => machines;

        public struct Machine
        {
            public string name;
            public AiStateMachine machine;
        }
#endif
    }
}