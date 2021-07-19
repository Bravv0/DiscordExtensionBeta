## Features

* Updates the bots status message on the discord server with text from the list at a set interval.

## Configuration

```json
{
  "Discord Application Bot Token": "",
  "Update Rate (Seconds)": 15.0,
  "Presence Messages": [
    {
      "Message": "on {server.name}",
      "Type": "Game"
    },
    {
      "Message": "{server.players}/{server.players.max} Players",
      "Type": "Game"
    },
    {
      "Message": "{server.players.sleepers} Sleepers",
      "Type": "Game"
    },
    {
      "Message": "{server.players.stored} Total Players",
      "Type": "Game"
    },
    {
      "Message": "Server FPS {server.fps}",
      "Type": "Game"
    },
    {
      "Message": "{server.entities} Entities",
      "Type": "Game"
    },
    {
      "Message": "{server.players.total} Lifetime Players",
      "Type": "Game"
    },
    {
      "Message": "{server.entities} Entities",
      "Type": "Game"
    },
    {
      "Message": "{server.players.queued} Queued",
      "Type": "Game"
    },
    {
      "Message": "{server.players.loading} Joining",
      "Type": "Game"
    },
    {
      "Message": "Wiped: {server.map.wipe.last!local}",
      "Type": "Game"
    },
    {
      "Message": "Size: {world.size} Seed: {world.seed}",
      "Type": "Game"
    }
  ],
  "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)": "Info"
}
```

### Valid Types;
`Game` - "Playing {name}"  
`Streaming` - "Streaming {name}"  
`Listening` - "Listening {name}"  
`Watching` - "Watching {name}"  
`Competing` - " Competing in {name}"

### Available Placeholders;

#### Note not all placeholders are available for every game. Please use `placeholderapi.list` to get the most up to date list for your game

#### Placeholder Structure:
`{key:format!option}`

- Key is the value displayed in the list below
- Format is the format to be applied to the returned value
- Option Allows you to change the type of data being returned

### Available Placeholders;

#### Note not all placeholders are available for every game. Please use `placeholderapi.list` to get the most up to date list for your game

#### Placeholder Structure:
`{key:format!option}`

- Key is the value displayed in the list below
- Format is the format to be applied to the returned value
- Option Allows you to change the type of data being returned

#### Placeholders

date.now (Placeholder API) - Options: "local" to use local time offset, UTC (default)   
`{date.now:MM/dd/yy hh:mm:ss tt}` - Will display the current date and time using the supplied format   
`{date.now:MM/dd/yy hh:mm:ss tt!local}` - Will do the same as above but convert the time to local time   
[DateTime Formatting](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings)

plugins.amount (Placeholder API)   
`{plugins.amount}` - returns the number of plugins on the server

server.address (Placeholder API)  
`{server.address}` - returns the server IP Address

server.blueprints.wipe.last (Placeholder API) - Options: "local" to use local time offset, UTC (default)    
`{server.blueprints.wipe.last:MM/dd/yy hh:mm:ss tt}` - Will display the last blueprint wipe date in UTC time  
`{server.blueprints.wipe.last:MM/dd/yy hh:mm:ss tt!local}` - Will display the last blueprint wipe date in local time    
[DateTime Formatting](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings)

server.blueprints.wipe.next (Placeholder API) - Options: "local" to use local time offset, UTC (default)    
`{server.blueprints.wipe.next:MM/dd/yy hh:mm:ss tt}` - Will display the next blueprint wipe date in UTC time  
`{server.blueprints.wipe.next:MM/dd/yy hh:mm:ss tt!local}` - Will display the next blueprint wipe date in local time    
[DateTime Formatting](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings)

server.description (Placeholder API)  
`{server.description}` - Will display the servers description

server.entities (Placeholder API)   
`{server.entities}` - Will return the number of entities on the server

server.fps (Placeholder API)    
`{server.fps:0}` - Will return the current server framerate

server.fps.average (Placeholder API)  
`{server.fps.average}` - Will return the average server framerate

server.language.code (Placeholder API)  
`{server.language.code}` - Returns Two letter ISO language name

server.language.name (Placeholder API)  
`{server.language.name}` - Returns the server language name

server.map.wipe.last (Placeholder API) - Options: "local" to use local time offset, UTC (default)    
`{server.map.wipe.last:MM/dd/yy hh:mm:ss tt}` - Will display the last map wipe date in UTC time  
`{server.map.wipe.last:MM/dd/yy hh:mm:ss tt!local}` - Will display the last map wipe date in local time    
[DateTime Formatting](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings)

server.map.wipe.next (Placeholder API) - Options: "local" to use local time offset, UTC (default)   
`{server.map.wipe.next:MM/dd/yy hh:mm:ss tt}` - Will display the next map wipe date in UTC time  
`{server.map.wipe.next:MM/dd/yy hh:mm:ss tt!local}` - Will display the next map wipe date in local time    
[DateTime Formatting](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings)

server.memory.total (Placeholder API) - Options: B (default), KB, MB, GB    
`{server.memory.total:0}` - Returns the servers total available memory in bytes  
`{server.memory.total:0!kb}` - Returns the servers total available memory in kilobytes  
`{server.memory.total:0!mb}` - Returns the servers total available memory in megabytes  
`{server.memory.total:0!gb}` - Returns the servers total available memory in gigabytes

server.memory.used (Placeholder API) - Options: B (default), KB, MB, GB    
`{server.memory.total:0.00}` - Returns the servers used available memory in bytes  
`{server.memory.total:0.00!kb}` - Returns the servers used available memory in kilobytes  
`{server.memory.total:0.00!mb}` - Returns the servers used available memory in megabytes  
`{server.memory.total:0.00!gb}` - Returns the servers used available memory in gigabytes

server.name (Placeholder API)   
`{server.name}` - Returns the servers host name

server.network.in (Placeholder API) - Options: B (or B/s, default) KB (or KB/s), MB (or MB/s), GB (or GB/s), Bps, Kbps, Mbps, Gbps    
`{server.network.in:0.00}` - Returns the servers network in in bytes  
`{server.network.in:0.00!KB}` - Returns the servers network in in kilobytes  
`{server.network.in:0.00!MB}` - Returns the servers network in in megabytes  
`{server.network.in:0.00!GB}` - Returns the servers network in in gigabytes

server.network.out (Placeholder API) - Options: B (or B/s, default) KB (or KB/s), MB (or MB/s), GB (or GB/s), Bps, Kbps, Mbps, Gbps    
`{server.network.out:0.00}` - Returns the servers network out in bytes  
`{server.network.out:0.00!KB}` - Returns the servers network out in kilobytes  
`{server.network.out:0.00!MB}` - Returns the servers network out in megabytes   
`{server.network.out:0.00!GB}` - Returns the servers network out in gigabytes

server.oxide.rust.version (Placeholder API)   
`{server.network.out}` - Returns the rust oxide version

server.players (Placeholder API)   
`{server.players}` - Returns the current player count on the server

server.players.loading (Placeholder API)  
`{server.players.loading}` - Returns the current number of players loading into the server

server.players.max (Placeholder API)   
`{server.players.max}` - Returns the maximum number of players allowed on the server

server.players.queued (Placeholder API)   
`{server.players.queued}` - Returns the number of players currently queued to join the server

server.players.sleepers (Placeholder API)   
`{server.players.sleepers}` - Returns the current number of sleepers on the server

server.players.stored (Placeholder API)
`{server.players.stored}` - Returns the total number of sleepers and connected players on the server

server.players.total (Placeholder API)   
`{server.players.total}` - Returns the total number of players that have ever been on the server

server.port (Placeholder API)  
`{server.port}` - Returns the server port

server.protocol (Placeholder API)     
`{server.protocol}` - Returns the server protocol version

server.protocol.network (Placeholder API)     
`{server.protocol.network}` - returns the server network version

server.protocol.persistance (Placeholder API)  
`{server.protocol.persistance}` - Returns the blueprint version for the server

server.protocol.report (Placeholder API)   
`{server.protocol.report}` - Returns the server report version

server.protocol.save (Placeholder API)   
`{server.protocol.save}` - Returns server save version

server.time (Placeholder API) - Current in-game time    
`{server.time:MM/dd/yy hh:mm:ss tt}` - Returns servers current time  
[DateTime Formatting](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings)

world.salt (Placeholder API)   
`{world.salt}` - Returns the servers salt

world.seed (Placeholder API)   
`{world.seed}` - Returns the servers map seed

world.size (Placeholder API) - Options: km, km2 (or km^2), m (default), m2 (or m^2)    
`{world.size}` - Returns map size in meters  
`{world.size!m2}` - Returns map size in meters squared  
`{world.size!km}` - Returns map size in kilometers  
`{world.size!km2}` - Returns map size in kilometers squared

world.url (Placeholder API)   
`{world.url}` - Returns the url to download the map