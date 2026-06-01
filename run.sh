#!bin/bash
export ConnectionStrings__DefaultConnection="Host=192.168.8.66;Port=5432;Database=RazlogDesserts;Username=postgres;Password=postgres"
export Mongo__ConnectionString="mongodb://192.168.8.66:27017/?connect=direct"
export Mongo__Database="RazlogDessertsChat"
export Mongo__MessagesCollection="messages"
dotnet watch run
