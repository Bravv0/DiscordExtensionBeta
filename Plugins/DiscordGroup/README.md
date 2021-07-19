## Features

* Adds a player to an oxide group or discord role when they link their discord and game accounts.
* Players can also be rewarded in game items which can reset on map wipe.

## Configuration

```json
{
  "Discord Bot Token": "",
  "Discord Server ID": "",
  "Add To Discord Role (Role ID)": "",
  "Add To Server Group": "",
  "Run Commands On Link": false,
  "Commands To Run": [
    "inventory.giveto {steamid} wood 100"
  ],
  "Reset Rewards On Wipe": false,
  "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)": "Info"
}
```

### Supported Command Replacements
`{steamid}` - Players Steam ID  
`{name}` - Players Name