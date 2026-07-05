try {
  rs.status();
} catch (e) {
  rs.initiate({
    _id: "rs0",
    members: [
      { _id: 0, host: "mongodb1:27017", priority: 2 },
      { _id: 1, host: "mongodb2:27017", priority: 1 },
      { _id: 2, host: "mongodb3-arbiter:27017", arbiterOnly: true }
    ]
  });
}
// Fulfill the Docker healthcheck by ending with a ping
db.adminCommand('ping');
