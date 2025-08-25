public class LiveStatusResponse
{
    public List<LiveStatusEntry> Latest { get; set; }
    public List<LiveStatusEntry> ALL { get; set; }
}

public class LiveStatusEntry
{
    public string Table { get; set; }
    public string Style { get; set; }
    public int Pass { get; set; }
    public int Fail { get; set; }
    public List<string> MostFails { get; set; }
    public List<MeasurementSummary> Measurements { get; set; }
}

public class MeasurementSummary
{
    public string Measurement { get; set; }
    public string Size { get; set; }
    public int Qty { get; set; }
    public int Trend { get; set; }
}