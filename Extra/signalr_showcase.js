const signalR = require("@microsoft/signalr");
const readline = require('readline');
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

async function connectToHub(token) {
  try {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5002/Leaderboard/Calories/Hub", {
        accessTokenFactory: () => token
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connection.on("ReceiveLeaderboard", (data) => {
      console.log(data);
    });

    await connection.start();
    console.log("Connected to hub");
  } catch (err) {
    console.error(err);
  }
}

rl.question("Enter your bearer auth token: ", (token) => {
  connectToHub(token)
  rl.close();
});
