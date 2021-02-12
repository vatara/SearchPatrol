namespace SearchPatrol.Common.SimObjects
{
    public class Boat : SimObject
    {
        enum Type { Boat01, Boat02 }

        public override string Random()
        {
            return $"{(Type)random.Next(0, 2)}";
        }
    }
}
