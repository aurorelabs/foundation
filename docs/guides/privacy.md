# Privacy of logs and traces

Personal data should not sit readable in logs and traces, and should still be readable by the people allowed to. With privacy on, the values of some
fields are encrypted on their way out of the process, and only the holder of the key can read them back.

```json
"RequestPipeline": {
  "Privacy": { "Enabled": true, "ProtectedFields": [ "customer.email" ] }
}
```

## What is protected

The client address, the peer address, the user agent and the user id (`client.address`, `network.peer.address`, `user_agent.original`, `enduser.id`), plus every
field named in `ProtectedFields` (any casing). Two places:

- **The log scope** the request middleware opens, so every log line of the request carries the encrypted values.
- **The tags of the spans**, encrypted when the span ends.

The `RequestContext` keeps the real values: your code can still read them, they only leave the process encrypted. Code that adds its own fields calls
`IPrivacyProtector.Protect(field, value)`, which encrypts the value when the field is protected and returns it unchanged otherwise.

## The tokens

A value becomes `base64url(nonce + ciphertext + tag)`, AES-256-GCM with a random nonce, so the same value gives a different token each time (tokens cannot be
matched with each other) and a tampered token is rejected. `IPrivacyProtector.Unprotect(token)` returns the value, or null for a malformed token, a tampered
one or one made with an unknown key.

## Keys

32 random bytes in base64 (`openssl rand -base64 32`). **Never in a file:** they come from the environment.

```
RequestPipeline__Privacy__Keys__Current=<key>
RequestPipeline__Privacy__Keys__Previous__0=<an old key>
```

New values are encrypted with the current key. To rotate, make the new key current and keep the old one under `Previous` for as long as you need to read the tokens it made;
removing it makes those tokens unreadable. The settings validator rejects privacy on without a valid current key, and a previous key that is not 32 base64 bytes.

## Reading and generating by hand

[`docs/scripts/privacy.cs`](../scripts/privacy.cs) needs only the .NET SDK.

```
dotnet run docs/scripts/privacy.cs -- generate-key
PRIVACY_KEYS=<current>,<previous> dotnet run docs/scripts/privacy.cs -- unprotect <token>
```

`PRIVACY_KEYS` is the current key and every previous one you still keep; each is tried in turn.
