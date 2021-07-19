# Discord Subscriptions

Allows plugins to register callbacks when a message is created in a specified channel.
This supports guild channels, guild categories, and direct messages.
When a plugin that has registered subscriptions is unloaded then the subscriptions will be removed.

## Getting Started

To be able to register channels for subscriptions you need to add the following line to your plugin.

```c#
private readonly DiscordSubscriptions _subscriptions = Interface.Oxide.GetLibrary<DiscordSubscriptions>();
```

## Callback

The callback for a subscription will be the DiscordMessage object.

```c#
void HandleMessage(DiscordMessage message)
```

## Registering Subscription

To register a subscription you can use the following example below

```c#
_subscriptions.AddChannelSubscription(this, snowflakeChannelId, callbackMethod);
```

## Unregistering Subscription

To unregister a subscription you can use the following example below

```c#
_subscriptions.RemoveChannelSubscription(this, snowflakeChannelId, callbackMethod);
```