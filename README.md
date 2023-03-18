# FitnessTrackerBackend
This is a backend application for a fitness tracker app

![Preview](https://user-images.githubusercontent.com/45824078/226111125-3ea6308a-9146-4bd6-9f91-81a9e97fcdd3.png)

## Setup
To run this application, use the following command: `docker-compose up`

Once the initialization is complete, Swagger Documentation can be accessed at `http:/localhost:5002/swagger/index.html`

## Technologies Used
- ASP.NET Core
- Redis
- JWT authentication
- Websocket (SignalR)
- Docker

## SignalR Showcase
A showcase JavaScript file can be found at `Extra/signalr_showcase.js`, it can be run with Node.js

This file demonstrates the usage of SignalR hub, which can be accessed at `http:/localhost:5002/Leaderboard/Calories/Hub`
