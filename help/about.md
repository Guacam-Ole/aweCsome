# What exactly is AweCsome?
AweCsome is *like* an Entity Framework but for SharePoint Lists using CSOM.

# A Simple example
Let's compare some simple tasks using the "traditional" approach and using AweCsome.

## Base Data
We want to run a successful car dealership. Therefore let's assume we have the following entities:
```csharp
public class Car {
   enum Colors {green,blue,orange}
   public string Manufacturer {get;set;}
   public Colors Color {get;set;}
   public string LicensePlate {get;set;}
   public DateTime BuyDate {get;set;}
   public DateTime? LastInspection {get;set;}
}

public class Customer {
  public string Name {get;set;}  
  public List<Car> Cars {get;set;}
}
```


## Creating lists
At first we will create lists for those entities in SharePoint using Csom. We assume we already have a ClientContext. Let's start with the `Car` - List:

### Classic approach using pure CSOM

```csharp
clientContext.Load(clientContext.Web);
clientContext.ExecuteQuery();
ListCreationInformation creationInfo = new ListCreationInformation();  
creationInfo.Title = "Car";              
creationInfo.TemplateType = (int) ListTemplateType.GenericList;  
List carList = clientContext.Web.Lists.Add(creationInfo);  
clientContext.ExecuteQuery();

string schemaManufacturer = "<Field Type='Text' Name='Manufacturer' DisplayName='Manufacturer' />";
Field manufacturer = carList.Fields.AddFieldAsXml(schemaManufacturer, true, AddFieldOptions.AddFieldInternalNameHint);
string schemaLicense = "<Field Type='Text' Name='Licenseplate' DisplayName='Licenseplate' />";
Field license = carList.Fields.AddFieldAsXml(schemaLicense, true, AddFieldOptions.AddFieldInternalNameHint);
string schemaBuydate = "<Field Type='DateTime' Name='Buydate' DisplayName='Buydate' required='TRUE' />";
Field buydate = carList.Fields.AddFieldAsXml(schemaBuydate, true, AddFieldOptions.AddFieldInternalNameHint);
string schemaInspection = "<Field Type='DateTime' Name='LastInspection' DisplayName='LastInspection'  />";
Field inspection = carList.Fields.AddFieldAsXml(schemaInspection, true, AddFieldOptions.AddFieldInternalNameHint);
string schemaColor = "<Field Type='Choice' Name='Color' DisplayName='Color' Format='Dropdown'>";
schemaColor+="<CHOICES><CHOICE>green</CHOICE><CHOICE>blue</CHOICE><CHOICE>orange</CHOICE></CHOICES>";
schemaColor+="</Field>";
Field color = carList.Fields.AddFieldAsXml(schemaColor, true, AddFieldOptions.AddFieldInternalNameHint);
clientContext.ExecuteQuery();
```
> *you don't need to use the XML-approach but have specific Methods for each Fieldtype like ```ChoiceField``` which makes it a BIT less > ugly, but it is the best approach when you try to automate that task lateron. Either way you have no connection to your entities.*

### Using AweCsome
Let's see what we have to do to create the same list using AweCsome:
```csharp
IAweCsomeTable aweCsomeTable = new AweCsome.AweCsomeTable(clientContext);
aweCsomeTable.CreateTable<Car>();
```

Much better to read, right? In both cases you have the same table at the end including all fields to be required where they should be and the choice field offering exactly the right options.
Just for completion, the second list:

### Second List, classic CSOM
Because this list has a lookup we need to retrieve the ID of that list first:
```csharp
clientContext.Load(clientContext.Web);
clientContext.ExecuteQuery();
List carList = clientContext.Web.Lists.GetByTitle("Car");
clientContext.Load(carList);
clientContext.ExecuteQuery();
Guid carListId= carList.Id;
```
And now we can create the ```Customer```-list and fields:

```csharp
ListCreationInformation creationInfo = new ListCreationInformation();  
creationInfo.Title = "Customer";              
creationInfo.TemplateType = (int) ListTemplateType.GenericList;  
List customerList = clientContext.Web.Lists.Add(creationInfo);  
clientContext.ExecuteQuery();

string schemaName = "<Field Type='Text' Name='Name' DisplayName='Name' />";
Field name = customerList.Fields.AddFieldAsXml(schemaName, true, AddFieldOptions.AddFieldInternalNameHint);
string schemaLookupField = $"<Field Type='LookupMulti' Name='Cars' DisplayName='Cars' List='{carListId}' ShowField='Title' Mult='TRUE' />"
Field lookupField = demoList.Fields.AddFieldAsXml(schemaLookupField, true, AddFieldOptions.AddFieldInternalNameHint);
clientContext.ExecuteQuery();
```

### Second List, AweCsome-approach
```csharp
IAweCsomeTable aweCsomeTable = new AweCsome.AweCsomeTable(clientContext);
aweCsomeTable.CreateTable<Customer>();
```
Yes. That's really it. Because ```List<Car>``` is a list of a complex type, AweCsome automatically detects that, retrieves the LookUp-Id and creates a MultiLookup.

## Inserting data into SharePoint-List
So we need to insert some data into the list. It would be helpful to have the Id stored somewhere so we add that to the car first:

```
class Car {
...
public int Id {get;set;}
}
```

So let's create some data first:
```csharp
var mx5=new Car {
  Manufacturer="Mazda",
  LicensePlate="HH-OA-1234",
  BuyDate=new DateTime(2019,4,34),
  Color=Colors.Green
}
var rx8=new Car {
  Manufacturer="Mazda",
  LicensePlate="HH-OD-13",
  BuyDate=new DateTime(2019,4,34),
  Color=Colors.Green,
  LastInspection=DateTime.Now
}
var Hodor=new Customer {
  Name="Hodor",
  Cars= new List<Car> {mx5,rx8}
}
```

Again let's start with the classic approach

### Classic CSOM 
Creating the mx5:
```csharp
clientContext.Load(clientContext.Web);
clientContext.ExecuteQuery();
List carList=clientContext.Web.Lists.GetByTitle("Car");
var itemCreateInfo=new ListItemCreationInformation();
ListItem mx5Item=carList.AddItem(itemCreateInfo);
mx5Item["Manufacturer"]=mx5.Manufacturer;
mx5Item["LicensePlate"]=mx5.LicensePlate;
mx5Item["BuyDate"]=mx5.BuyDate;
mx5Item["Color"]=mx5.Color.ToString();
mx5Item["LastInspection"]=mx5.LastInspection;
mx5Item.Update();
clientContext.ExecuteQuery();
mx5.Id=mx5Item.Id;
```
Creating the rx8:
```csharp
clientContext.Load(clientContext.Web);
clientContext.ExecuteQuery();
List carList=clientContext.Web.Lists.GetByTitle("Car");
var itemCreateInfo=new ListItemCreationInformation();
ListItem rx8Item=carList.AddItem(itemCreateInfo);
rx8Item["Manufacturer"]=rx8.Manufacturer;
rx8Item["LicensePlate"]=rx8.LicensePlate;
rx8Item["BuyDate"]=rx8.BuyDate;
rx8Item["Color"]=rx8.Color.ToString();
rx8Item["LastInspection"]=rx8.LastInspection;
rx8Item.Update();
clientContext.ExecuteQuery();
rx8.Id=rx8Item.Id;
```
Creating the Customer:
```csharp
clientContext.Load(clientContext.Web);
clientContext.ExecuteQuery();
List carList=clientContext.Web.Lists.GetByTitle("Car");
var itemCreateInfo=new ListItemCreationInformation();
ListItem hodorItem=carList.AddItem(itemCreateInfo);
hodorItem["Name"]=hodor.Name;
hodorItem["Cars"]=hodor.Cars.Select(q=>q.Id).ToArray();
hodorItem.Update();
clientContext.ExecuteQuery();
```

### Using AweCsome
So let's insert the the same data using AweCsome
```csharp
IAweCsomeTable aweCsomeTable = new AweCsome.AweCsomeTable(clientContext);
mx5.Id=aweCsomeTable.InsertItem(mx5);
rx8.Id=aweCsomeTable.InsertItem(rx8);
aweCsomeTable.InsertItem(hodor);
```
That was easy, wasn't it? 
Well, to be honest, I cheated a little bit. I added the ```Id``` field to the entities **after** I created the list in SharePoint. If I had them there before, AweCsome would not have been able to create the lists.
The reason is simple: *every* Property creates a Field by default. But if there already is an internal field with the same name (like "Id") we have a conflict and AweCsome aborts creating that list. Thankfully we have some attributes for that purpose:
```csharp
[IgnoreOnCreation, IgnoreOnInsert, IgnoreOnUpdate]
public int Id {get;set;}
```
This prevents the Id from being created, inserted or updated - which makes sense as the Id is completely maintained by SharePoint itself which does not really like it if we fiddle with that value.
You can also simply change the class to
```csharp
public class Car:AweCsomeListItem {...}
```
This way AweCsome already takes care of all important fields from Custom Lists like ```Id```, ```Title```, ```Author``` and so on.

## Updating and Selecting data
I will not bother you by writing down classic CSOM-Examples for Update and Select. They are as long and ugly as inserting data like shown above. In the following AweCsome-example we retrieve every car and schedule the Inspection for next week:

```csharp
IAweCsomeTable aweCsomeTable = new AweCsome.AweCsomeTable(clientContext);
var allCars=aweCsomeTable.SelectAllItems<Car>();
foreach (var car in allCars) {
  car.LastInspection=DateTime.Now.AddDays(7);
  aweCSomeTable.Update(car);
}
```

## Convinced?
Hope you liked those small examples. If you decide to use AweCsome check the [Wiki](https://github.com/OleAlbers/aweCsome/wiki) how to use it and what other fancy things you can do.

