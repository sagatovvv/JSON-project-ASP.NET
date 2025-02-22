using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder();
var app = builder.Build();

app.Run(async (context) =>
{
    var response = context.Response;
    var request = context.Request;

    if (request.Path == "/api/user")
    {
        var responseText = "Некорректные данные";
        if (request.HasJsonContentType())
        {
            var jsonoptions = new JsonSerializerOptions();
            jsonoptions.Converters.Add(new PersonConverter());

            var person = await request.ReadFromJsonAsync<Person>(jsonoptions);
            if (person != null && person.Age >= 0)
                responseText = $"Name: {person.Name}  Age: {person.Age}";
        }
        await response.WriteAsJsonAsync(new { text = responseText });
    }
    else
    {
        response.ContentType = "text/html; charset=utf-8";
        await response.SendFileAsync("html/ind.html");
    }
});

app.Run();

public record Person(string Name, int Age);

public class PersonConverter : JsonConverter<Person>
{
    public override Person Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var personName = "Undefined";
        var personAge = 0;
        bool hasValidAge = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();
                switch (propertyName?.ToLower())
                {
                    case "age" when reader.TokenType == JsonTokenType.Number:
                        int ageValue = reader.GetInt32();
                        if (ageValue >= 0)
                        {
                            personAge = ageValue;
                            hasValidAge = true;
                        }
                        break;
                    case "age" when reader.TokenType == JsonTokenType.String:
                        string? stringValue = reader.GetString();
                        if (int.TryParse(stringValue, out int parsedAge) && parsedAge >= 0)
                        {
                            personAge = parsedAge;
                            hasValidAge = true;
                        }
                        break;
                    case "name":
                        string? name = reader.GetString();
                        if (name != null)
                            personName = name;
                        break;
                }
            }
        }

        if (!hasValidAge)
        {
            throw new JsonException("Возраст не может быть отрицательным.");
        }

        return new Person(personName, personAge);
    }

    public override void Write(Utf8JsonWriter writer, Person person, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("name", person.Name);
        writer.WriteNumber("age", person.Age);
        writer.WriteEndObject();
    }
}
