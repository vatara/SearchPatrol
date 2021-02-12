using Xunit;

namespace SearchPatrol.Common.Tests
{
    public class SearchPatrolMainTests
    {
        [Theory]
        [InlineData(0, 0, 90, 20000, 0, .17966305685)]
        [InlineData(0, 0, 0, 20000, .18087388938, 0)]
        [InlineData(45, -90, 270, 20000, 44.99971877184, -90.25365551838)]
        [InlineData(45, -90, 335, 20000, 45.163052, -90.10751)]
        public void HeadingDistanceToCoords_Works(double lat, double lon, double heading, double distance, double endLat, double endLon)
        {
            var result = SearchPatrolMain.HeadingDistanceToCoords(lat, lon, heading, distance);
            Assert.Equal(endLat, result.Latitude, 5);
            Assert.Equal(endLon, result.Longitude, 5);
        }
    }
}
