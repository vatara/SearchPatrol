namespace SearchPatrol.Common.SimObjects
{
    public class Animal : SimObject
    {
        public enum Type
        {
            BlackBear,
            GrizzlyBear,
            PolarBear,
            SyrianBear,
            AfricanElephant,
            AfricanElephant_Child,
            AsianElephant,
            AsianElephant_Child,
            BorneoElephant,
            BorneoElephant_Child,
            SriLankanElephant,
            SriLankanElephant_Child,
            SriLankanElephant_Child_Albino,
            SumatranElephant,
            SumatranElephant_Child,
            SumatranElephant_Child_Albino,
            AfricanGiraffe,
            AngolanGiraffe,
            MasaiGiraffe,
            ReticulatedGiraffe,
            Hippo,
            RhinoBlack,
            RhinoBlackWestern,
            RhinoWhite,
            RhinoWhiteNorthern,
            //Flamingo,
            //Goose,
            //Seagull
        }

        public override string Random()
        {
            return $"{(Type)random.Next(0, (int)Type.RhinoWhiteNorthern + 1)}";
        }
    }
}
