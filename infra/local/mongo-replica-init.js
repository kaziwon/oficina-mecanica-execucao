try {
  const status = rs.status();
  if (status.set !== "rs0") throw new Error("Replica set existente diferente de rs0; nao sera reconfigurado.");
} catch (error) {
  if (error.code !== 94) throw error;
  const result = rs.initiate({ _id: "rs0", members: [{ _id: 0, host: "mongo:27017" }] });
  if (!result.ok) throw new Error(JSON.stringify(result));
}
let pronto = false;
for (let tentativa = 0; tentativa < 60; tentativa++) {
  if (db.hello().isWritablePrimary) { pronto = true; break; }
  sleep(1000);
}
if (!pronto) throw new Error("MongoDB nao elegeu o primary do replica set.");
print("Replica set rs0 pronto para transacoes locais.");
