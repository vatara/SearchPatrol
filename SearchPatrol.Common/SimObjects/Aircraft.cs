namespace SearchPatrol.Common.SimObjects
{
    public class Aircraft : SimObject
    {
        readonly string[] types = new string[]
        {
            "Cessna 152 Asobo",
            "DA62 Asobo"
        };

        public override string Random()
        {
            return types[random.Next(0, types.Length)];
        }
    }
}
