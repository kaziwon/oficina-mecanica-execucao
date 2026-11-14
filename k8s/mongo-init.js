const databaseName = process.env.MONGO_DATABASE;
db.getSiblingDB(databaseName).createUser({
  user: process.env.MONGO_USERNAME,
  pwd: process.env.MONGO_PASSWORD,
  roles: [{ role: "readWrite", db: databaseName }],
});
