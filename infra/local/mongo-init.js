const databaseName = process.env.MONGO_DATABASE;
const appDatabase = db.getSiblingDB(databaseName);

appDatabase.createUser({
  user: process.env.MONGO_USERNAME,
  pwd: process.env.MONGO_PASSWORD,
  roles: [{ role: "readWrite", db: databaseName }],
});
