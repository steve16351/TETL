# TETL
Textfile Extract, Transform and Load is a lightweight .NET library to facilitate text file serialisation to objects, and then on to database endpoints. It is designed to handle large amounts of data by dealing with streams, so it is able to process the file row by row performing operations as required rather than having to have the entire file in memory at any one time.

You might want to use this if using SSIS to load and transform your data isn't practical due to licencing concerns, or you'd rather not have a pure .NET solution for a simple ETL task.

## Supported Runtimes
- .NET Framework 4.6.1+
## Supported Database Targets
- MS SQL Server

## Basic usage

Given a text file you wish to deserialize to a .NET object:

```
Name;Weight;Height
Fred;71.3;165
Andy;80.2;180
Jane;63.5;160
```

### 1 First Decorate your class with the TextFileMappingAttribute
```csharp
public class WeightEntry
{
    [TextFileMappingAttribute(ColumnName = "Name")]
    public string Name { get; set; }
    [TextFileMappingAttribute(ColumnName = "Weight")]
    public double Weight { get; set; }
    [TextFileMappingAttribute(ColumnName = "Height")]
    public int Height { get; set; }
}
```

### 2 Create an instance of the TextFileSerializer
```csharp
TextFileSerializer<MockData> textFileSerializer = new TextFileSerializer<MockData>(@"c:\temp\myTextFile")
{
    Delimiter = ";",
    FirstRowHeader = true
};
```

### 3 Consume the data via enumeration
```csharp
foreach (WeightEntry data in textFileSerializer)
  Console.WriteLine($"Name = {data.Name}, Height = {data.Height}; Weight = {data.Weight}");
```
