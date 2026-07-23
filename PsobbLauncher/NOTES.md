# Hangame Mode — Companion ASI

Server profiles with **Hangame mode** enabled require a companion ASI patch
to be present in the game's `patches/` folder (or game root). The launcher
itself only writes the credential handoff; the ASI applies the in-memory
patches that put the client into Hangame login mode.

## Where to get it

Source and build instructions: https://github.com/Silus-Wyvern/Hangame

Build the project, then place the resulting `Hangame.asi` in your PSOBB
`patches/` folder. patch.dll's built-in ASI loader picks it up automatically.

## How the two halves connect

- **Standard profiles** use the existing registry capture-replay path. The
  ASI does nothing.
- **Hangame profiles** bypass the registry (per newserv issue #401). On
  launch the launcher writes `hangame.ini` next to `psobb.exe` with the
  decrypted credentials. The ASI reads it on load, applies the Hangame
  memory patches, then deletes the file. If `hangame.ini` is absent the ASI
  no-ops, so the same ASI is harmless on standard launches.

## Credential format (enforced by the launcher)

- Username must end in `@HG`, max 11 characters.
- Password must be numeric, 1–8 digits.
