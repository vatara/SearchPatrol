namespace SearchPatrol.Common.SimObjects
{
    public class GroundVehicle : SimObject
    {
        enum Type
        {
            ASO_Aircraft_Caddy,
            ASO_Baggage_Cart01,
            ASO_Baggage_Cart02,
            Aso_Baggage_Loader_01,
            ASO_BaggageTruck01,
            aso_loadercab,
            ASO_Boarding_Stairs,
            ASO_Boarding_Stairs_Red,
            ASO_Boarding_Stairs_Yellow,
            ASO_CarFacillity01_Black,
            ASO_CarFacillity01_White,
            ASO_TruckFacility01_Black,
            ASO_TruckFacility01_White,
            ASO_TruckFacility01_Yellow,
            ASO_CarUtility01,
            ASO_TruckUtility01,
            ASO_Catering_Truck_01,
            ASO_Firetruck01,
            ASO_Firetruck02,
            ASO_FuelTruck01_Black,
            ASO_FuelTruck01_White,
            ASO_FuelTruck02_Black,
            ASO_FuelTruck02_White,
            ASO_Ground_Power_Unit,
            aso_operation_truck,
            ASO_Pushback_Blue,
            ASO_Pushback_White,
            ASO_Shuttle_01_Gray,
            ASO_Shuttle_01_Yellow,
            ASO_Tug01_White,
            ASO_Tug02_White
        }

        public override string Random()
        {
            return $"{(Type)random.Next(0, (int)Type.ASO_Tug02_White + 1)}";
        }
    }
}
