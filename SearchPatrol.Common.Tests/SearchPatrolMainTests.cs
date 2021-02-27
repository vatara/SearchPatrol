using System;
using System.Collections.Generic;
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

        [Fact]
        public void WaveDetect_Works()
        {
            var bankHistory = new List<Tuple<DateTimeOffset, double>>()
            {
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, -20),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 0),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 20),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 0),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, -20),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 0),
            };

            Assert.True(SearchPatrolMain.DetectWingWave(bankHistory, 15));
        }

        [Fact]
        public void WaveDetect_WorksReverse()
        {
            var bankHistory = new List<Tuple<DateTimeOffset, double>>()
            {
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 20),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 0),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, -20),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 0),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 20),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 0),
            };

            Assert.True(SearchPatrolMain.DetectWingWave(bankHistory, 15));
        }

        [Fact]
        public void WaveDetect_ReturnsFalseWhenLowAngles()
        {
            var bankHistory = new List<Tuple<DateTimeOffset, double>>()
            {
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, -20),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 0),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 14),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 0),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, -20),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 0),
            };

            Assert.False(SearchPatrolMain.DetectWingWave(bankHistory, 15));
        }

        [Fact]
        public void WaveDetect_ReturnsFalseWhenSameDirections()
        {
            var bankHistory = new List<Tuple<DateTimeOffset, double>>()
            {
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 20),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 0),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 20),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 0),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 20),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 0),
            };

            Assert.False(SearchPatrolMain.DetectWingWave(bankHistory, 15));
        }

        [Fact]
        public void WaveDetect_ReturnsFalseWhenSameDirectionsNegative()
        {
            var bankHistory = new List<Tuple<DateTimeOffset, double>>()
            {
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, -20),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 0),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, -20),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 0),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, -20),
                new Tuple<DateTimeOffset, double>(DateTimeOffset.Now, 0),
            };

            Assert.False(SearchPatrolMain.DetectWingWave(bankHistory, 15));
        }
    }
}
