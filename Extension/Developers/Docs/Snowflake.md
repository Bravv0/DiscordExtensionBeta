# Snowflake

A [Snowflake](https://discord.com/developers/docs/reference#snowflakes) is the name of the type used for the ID field in discord entities. 
A snowflake struct in the Discord Extension is comprised of a ulong and be be casted to and from a ulong.
The snowflake struct allows a user to get the creation date for an entity.
Since a snowflake is a struct it is never null unless otherwise specified.
The default value for a snowflake is 0.
This is considered an invalid value and you can check if a snowflake is valid by calling `.IsValid()`

## Entity Created Date

To get the date the entity was created you can call the following method on a snowflake
`.GetCreationDate()` which returns a `DateTimeOffset`

## Serialization

The snowflake already comes with a JSON Serializer attached to it.
When it serializes a snowflake to JSON it will write the ulong value as a string
This is because that is how it is specified in the discord documentation.
This makes a snowflake compatible with Discord Extension Id's prior to version 2.0.0  
**Note:** Instead of writing 0 for invalid snowflakes an empty string is written when serialized

## Casting & Converting

A snowflake can be casted and converted between ulong and string.

If you wish to convert to and from a ulong you can use the following code
```c#
ulong userId = 1234567890;
Snowflake sfUserId = (Snowflake)userId;
ulong backToUserId = (ulong)sfUserId;
```

If you wish to convert from a string to a snowflake you can use the TryParse method
```c#
string id = "1234567890";
Snowflake userId;
if(!Snowflake.TryParse(id, out userId))
{
    //Failed to parse snowflake
}
```