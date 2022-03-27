// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
using System.Collections.Generic;

public class CoverageStat
{
    public string label { get; set; }
    public int position { get; set; }
    public int total { get; set; }
    public int covered { get; set; }
    public bool isDeltaAvailable { get; set; }
    public double delta { get; set; }
}

public class CoverageData
{
    public List<CoverageStat> coverageStats { get; set; }
    public string buildPlatform { get; set; }
    public string buildFlavor { get; set; }
}

public class Build
{
    public string id { get; set; }
    public string url { get; set; }
}

public class Root
{
    public List<CoverageData> coverageData { get; set; }
    public Build build { get; set; }
    public object deltaBuild { get; set; }
    public string status { get; set; }
}

