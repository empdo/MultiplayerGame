mcs -target:library -out:./myLibrary.dll ./Client.cs ./Server.cs ./PacketStream.cs
mcs -reference:./myLibrary.dll ./Main.cs

mono ./Main.exe
