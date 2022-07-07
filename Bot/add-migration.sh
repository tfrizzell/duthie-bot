#!/bin/bash

NAME=$1
dotnet ef migrations add "$NAME" --project ../Migrations/Sqlite -- --provider sqlite --connection-string "Data Source=duthie-bot.db"
dotnet ef migrations add "$NAME" --project ../Migrations/Mysql -- --provider mysql --connection-string "server=localhost;port=3306;database=duthie;user=duthie;password=duthie"