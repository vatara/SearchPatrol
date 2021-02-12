namespace SearchPatrol.Common.SimObjects
{
    public class CargoShip : SimObject
    {
        enum Type { CargoContainer01, CargoGas01, CargoOil, CargoShip01 }

        public override string Random()
        {
            return $"{(Type)random.Next(0, (int)Type.CargoShip01 + 1)}";
        }
    }
}
