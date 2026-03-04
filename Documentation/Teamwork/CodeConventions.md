# C# Code conventions

## File Organization

### File and Folder Names

**File** and **folder** names are written in UpperCamelCase.

```
Scripts/
└── Player/
    └── Movement/
        ├── PlayerController.cs
        ├── PlayerInput.cs
```

### Namespaces

z
Namespaces are written in UpperCamelCase and are based on the folder name the script is placed in. The namespace should reflect the directory where the class is stored using the following layout. Every class needs to be inside of a namespace.

```csharp
namespace ExampleProject.Player.Movement
{
    public class PlayerController
    {

    }
}
```

### Line length

Code lines are not allowed to exceed **120 characters** so that code will always be readable on most screen sizes.

## Code Structure

### Classes

Class names are written in UpperCamelCase and should be short and consise.

```csharp
public class ExampleClass : InheritedClass
{

}
```

### Bracket Placement

**Brackets** are placed on a new line and **not** on the same line.

```csharp
public class ExampleClass
{

}
```

### Structs

Struct names are written in UpperCamelCase.

```csharp
public struct ExampleStruct
{
    public double x;
    public double y;
}
```

### Enums

Enum names are written in UpperCamelCase while the constants inside the enum are written in FULL_CAPITALS.

```csharp
public enum ExampleEnum
{
    FIRST_CONSTANT,
    SECOND_CONSTANT
}
```

## Naming Conventions

### Functions

Functions are written in UpperCamelCase.

```csharp
private void ExampleFunction()
{

}
```

**Parameters** for the function are always written in lowerCamelCase

```csharp
private void ExampleFunction(string exampleParameter)
{

}
```

### Variables

**Private** variables always start with the prefix `_` after which it's written in lowerCamelCase.

```csharp
private Object _privateExample;
```

**Public** variables are written in lowerCamelCase and don't need a prefix.

```csharp
public Object publicExample;
```

**Readonly** variables are written with the above given rules by their access modifier

```csharp
public readonly Object publicExample;
private readonly Object _privateExample;
```

**Constant** variables are written in FULL_CAPITALS.

```csharp
public const int PUBLIC_CONSTANT_VALUE;
private const int PRIVATE_CONSTANT_VALUE;
```

**Internal** variables always start with the prefix 'i\_' after which it's written in lowerCamelCase.

```csharp
internal int i_internalExample;
```

**Protected** variables always start with the prefix 'p\_' after which it's written in lowerCamelCase. And are never serializable in the inspector.

```csharp
protected int p_protectedExample;
```

**Temporary** variables (inside of an function) always need to be written out fully and are written in lowerCamelCase.

```csharp
private void ExampleFunction()
{
    float temporaryFloat = 1f;
    int temporaryInt = 1;
}
```

**Temporary** constants inside of an function always need to be written out and are written in FULL_CAPITALS.

```csharp
private void ExampleFunction()
{
    const float TEMPORARY_CONSTANT_FLOAT = 1f;
    const int TEMPORARY_CONSTANT_INT = 1;
}
```

**Properties** are written in UpperCamelCase.

```csharp
public int ExampleInteger
{
    get => _exampleInterger;
    set
    {
        if (value < 0)
            _exampleInterger = 0;
    }
}

public int SecondExampleInterger => _secondExampleInteger;
```

**Lists** are written the same as public or private variables.

```csharp
private List<GameObject> _exampleList = new();
```

**Actions** are written in UpperCamelCase and needs to start with the prefix `On`.

```csharp
public Action OnExampleAction;
public Action<int> OnSecondExample;
```

## Documentation

### Summaries

Public functions require a summary which concisely explains what the function does. This is so when accessing the function from another class it's easy to see it functionality.

```csharp
/// <summary>
/// A summary explaining the functionality of the function.
/// </summary>
public void ExampleFunction()
{

}
```

## Readability

### nesting

as a general rule it is best to keep nesting to a minimum. The more nested a code is, the more things you have to keep track of. 

there are two ways to reduce nesting:

if statement inversion:
```csharp
private void MovePlayer()
{
    if(player != null)
    {
        player.position.x += 1;
    }
}
```
//can also be written as:
```csharp
private void MovePlayer()
{
    if(player == null) return;
    
    player.position.x += 1;
}
```
and extracting code to a new function:
```csharp
public float ProcessOrders(List<Order> orders)
{
    float total = 0;
    foreach (var order in orders)
    {
        if (order.IsPaid)
        {
            foreach (var item in order.Items)
            {
                total += item.Price * item.Quantity;
            }
        }
    }
    return total;
}
```
can also be written as:
```csharp
public float ProcessOrders(List<Order> orders)
{
    float total = 0;
    foreach (var order in orders)
    {
        if (!order.IsPaid) continue;

        total += CalculateOrderTotal(order);
    }
    return total;
}

private float CalculateOrderTotal(Order order)
{
    float total = 0;
    foreach (var item in order.Items)
    {       
        total += item.Price * item.Quantity;
    }
    return total;
}
```