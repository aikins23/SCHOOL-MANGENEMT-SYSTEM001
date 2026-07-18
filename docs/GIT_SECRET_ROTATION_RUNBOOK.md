# Git Secret Rotation Runbook

## Purpose

Use this runbook after a credential has been committed to Git. Rotating the live
credential stops future use, but the retired value must also be removed from every
reachable branch and tag before the repository is considered clean.

## Implemented Controls

- `Scripts/Scan-RepositorySecrets.ps1` scans the working tree and all reachable Git
  blobs without printing secret values.
- `.github/workflows/secret-scan.yml` runs the scan for every branch push, pull
  request, and manual dispatch.
- `Scripts/Rewrite-RetiredGitSecrets.py` rewrites only fingerprints listed in
  `Scripts/retired-secret-fingerprints.json`.
- Certificate private keys, local secret files, and rewrite workspaces are ignored.

## Verified Rewrite Procedure

1. Rotate the live credential first and prove the retired credential is rejected.
2. Clone the repository into an isolated local mirror.
3. Create a local bundle containing the pre-rewrite remote branch tips.
4. Run the fingerprint-targeted fast-export/fast-import rewrite.
5. Run `git fsck --full --strict` against the cleaned mirror.
6. Run the full history scanner against the cleaned mirror.
7. Compare old and rewritten branch commit counts and tip trees.
8. Confirm remote branch tips have not changed since the local mirror was created.
9. Force-update only the known remote branches using explicit refspecs.

Never push rewrite backup refs, local stashes, remote-tracking refs, or the local
pre-rewrite bundle. They intentionally retain the retired history for emergency
local recovery and must be stored securely, then deleted after the rewrite is
accepted.

## Collaborator Recovery

After the force update, every collaborator must make a fresh clone. Do not merge an
old local branch into the cleaned repository because that reintroduces the retired
objects. Any work that exists only in an old clone must be exported as a patch and
reviewed before it is applied to the fresh clone.

## Release Gate

The secret scan must pass for the working tree and complete reachable history. A
failure reports only the rule, path, line, object ID, and a short SHA-256
fingerprint. Secret values must never be copied into an issue, build log, or chat.
