﻿using System.Collections.Generic;
using System.IO;
using Xunit;

namespace CSCJConverter.tests;

public class CityJSONFixture
{
    public CityJSON cityJson { get; private set; }
    
    public CityJSONFixture()
    {
        string testFile = @"testfixtures/test_shed.json";
        string jsonString = File.ReadAllText(testFile);
        this.cityJson = new CityJSON(jsonString, "testfixtures/test.json");
    }
}

public class MaaiveldConversionTests : IClassFixture<CityJSONFixture>
{
    private readonly CityJSON cityJson;
    
    public MaaiveldConversionTests(CityJSONFixture fixture)
    {
        this.cityJson = fixture.cityJson;
    }
    
    /// <summary>
    /// Test function which converts a 'real' value in meters (decimal) to a translated and scaled CityJSON value.
    /// </summary>
    [Fact]
    public void HeightMetersToCityJSON_ReturnsCorrectValue()
    {
        const decimal inputValue = 12.3456789m;
        // De formule om van CityJSON naar echte waarde te gaan:
        // v[2] = (vi[2] * ["transform"]["scale"][2]) + ["transform"]["translate"][2]
        // Onze scale is 0.001 en de translate is 0.
        // We verwachten dus: afronden((12.3456789 - 0) / 0.001) = 12346
        const int expectedValue = 12346;

        int result = this.cityJson.HeightMetersToCityJSON(inputValue);
        
        Assert.Equal(expectedValue, result);
    }

    /// <summary>
    /// Test the function which modifies vertices.
    /// It will use the h_maaiveld value, convert it to CityJSON value (by applying scale and translate)
    /// and then modify all vertices belonging to the building.
    /// </summary>
    [Fact]
    public void TestModifyHeight()
    {
        // Arrange
        int expectedVerticesUpdated = 20;
        IEnumerable<int> expectedVertexZero = new int[3] {-38707, 156058, 0};
        IEnumerable<int> expectedVertexTwo = new int[3] {-36549, 156471, 0};
        IEnumerable<int> expectedVertexSeventeen = new int[3] { -35747, 154248, 2371 };
        IEnumerable<int> expectedVertexNineteen = new int[3] {-35724, 154355, 2364};
        // Control vertex (index 20) doesn't belong to any building, thus shouldn't have been modified
        IEnumerable<int> expectedVertexTwenty = new int[3] {155380, 287061, 30234};
        
        // Act
        var res = this.cityJson.TranslateHeightMaaiveld();
        
        // Assert
        Assert.Equal(expectedVerticesUpdated, res);
        Assert.Equal(expectedVertexZero, this.cityJson.CityJson.vertices[0]);
        Assert.Equal(expectedVertexTwo, this.cityJson.CityJson.vertices[2]);
        Assert.Equal(expectedVertexSeventeen, this.cityJson.CityJson.vertices[17]);
        Assert.Equal(expectedVertexNineteen, this.cityJson.CityJson.vertices[19]);
        Assert.Equal(expectedVertexTwenty, this.cityJson.CityJson.vertices[20]);
    }

    /// <summary>
    /// Test if the bbox is updated correctly. Z-values should have been corrected while x,y should be unchanged.
    /// Originally it was minz= -5.083 and maxz=-2.362
    /// Moving to 0-based we expect minz=0 and maxz=2.721
    /// </summary>
    [Fact]
    public void TestTransformGeographicalExtentZToZero()
    {
        double[] expectedArray = new double[6] { 97816.25, 438731.53, 0, 97819.29000000001, 438734.469, 2.721 };
        
        this.cityJson.TransformGeographicalExtentZToZero();
        
        Assert.Equal(expectedArray, this.cityJson.CityJson.metadata.geographicalExtent);
    }
}