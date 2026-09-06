UNITY IMPORT NOTES
==================
Assets: 6 color variants of a panel jack / banana-style connector.
Files: JackConnector_Yellow/Red/Blue/Black/Green/White.fbx

Recommended Unity import:
- Scale Factor: 1
- Convert Units: ON
- Import Materials: ON
- Mesh Compression: Low/Off
- Read/Write: OFF unless runtime mesh edits are needed
- Generate Colliders manually (Box/Capsule is usually enough)
- Front socket points along local +Z.
- Approx. outside diameter: 31 mm.
- Designed as a simple low-poly/medium-poly training asset, not a mechanical CAD model.

Hierarchy in each FBX:
JackConnector_<Color>
  ColorCollar
  MetalRim
  Socket
  RearHousing
  BrassBarrel
  HexNut
  ThreadRidge1..3

Tip:
Create a prefab and add an empty child named SnapPoint at the center of the front socket.
Use SnapPoint for cable-end snapping in the Digital Twin interaction.
