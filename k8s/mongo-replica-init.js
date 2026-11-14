const host = process.env.MONGO_REPLICA_HOST;
if (!host) throw new Error("MONGO_REPLICA_HOST nao configurado.");
try {
  const configuration = rs.conf();
  if (configuration._id !== "rs0" || configuration.members.length !== 1 || configuration.members[0].host !== host) {
    throw new Error("Replica set existente diferente do esperado; nao sera reconfigurado automaticamente.");
  }
} catch (error) {
  if (error.code !== 94) throw error;
  const result = rs.initiate({ _id: "rs0", members: [{ _id: 0, host }] });
  if (!result.ok) throw new Error(JSON.stringify(result));
}
let ready = false;
for (let attempt = 0; attempt < 90; attempt++) {
  if (db.hello().isWritablePrimary) { ready = true; break; }
  sleep(1000);
}
if (!ready) throw new Error("MongoDB nao elegeu um primary.");
print("Replica set rs0 pronto para transacoes.");
