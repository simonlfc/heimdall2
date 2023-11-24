cd Protobuf

for %%i in (*.proto) do protoc --csharp_out=. %%i