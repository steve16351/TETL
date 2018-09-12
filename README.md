# TETL
Textfile Extract, Transform and Load is a lightweight .NET library to facilitate text file serialisation to objects, and then on to database endpoints. It is designed to handle large amounts of data.

## Supported Runtimes
- .NET Framework 4.6.1+

## Basic usage

Given a text file you wish to deserialize to a .NET object:

> Name;Weight;Height
> Fred;71.3;165
> Andy;80.2;180
> Jane;63.5;160

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
