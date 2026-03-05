namespace TeslaLogger
{
    internal partial class Car
    {

        internal enum TeslaState
        {
            Start,
            Drive,
            Park,
            Charge,
            Sleep,
            WaitForSleep,
            Online,
            GoSleep,
            Inactive
        }

        private TeslaState _currentState = TeslaState.Start;
    }
}
