# Telephone chat art

Active portrait: `Assets/Resources/Characters/NinaPortraitSelected.png`.
Copied unchanged from the image selected by the user. Its background and text
are preserved. The two previous generated portrait assets and their Unity meta
files were removed at the user's request.

UI: `Assets/Scripts/PhoneChatView.cs` and `Assets/Resources/PhoneChat.uss`.
The portrait is displayed with its complete UV range and ScaleToFit. Its importer
preserves the source 1448×1086 aspect (NPOT scaling disabled), and the panel height
adapts to the image and available window space.
