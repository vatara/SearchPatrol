namespace SearchPatrol.Common.SimObjects
{
    public class CruiseShip : SimObject
    {
        enum Type { CruiseShip, CruiseShip02 }

        public override string Random()
        {
            return $"{(Type)random.Next(0, 2)}";
        }
    }
}
