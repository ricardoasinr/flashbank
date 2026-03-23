/**
 * FlashBank - Semilla MongoDB (read model historial de transacciones / CQRS)
 * Se ejecuta solo en el primer arranque con volumen vacío (docker-entrypoint-initdb.d).
 * BD: MONGO_INITDB_DATABASE (mongodb-history). Colección: history.
 */

const dbName = 'mongodb-history';
const collName = 'transaction-history';
const historyDb = db.getSiblingDB(dbName);

if (historyDb[collName].countDocuments({ source: 'docker-seed' }) > 0) {
  print(`Mongo seed [${dbName}.${collName}]: semilla ya presente. Omitiendo.`);
} else {
  const acc1 = UUID();
  const acc2 = UUID();
  const acc3 = UUID();
  const now = new Date();
  const daysAgo = (d) => new Date(now.getTime() - d * 24 * 60 * 60 * 1000);

  historyDb[collName].insertMany([
    {
      transactionId: UUID(),
      accountId: acc1,
      amount: NumberDecimal('2500.00'),
      type: 'Deposit',
      status: 'Completed',
      occurredAt: daysAgo(5),
      source: 'docker-seed',
    },
    {
      transactionId: UUID(),
      accountId: acc1,
      amount: NumberDecimal('150.75'),
      type: 'Withdrawal',
      status: 'Completed',
      occurredAt: daysAgo(4),
      source: 'docker-seed',
    },
    {
      transactionId: UUID(),
      accountId: acc2,
      amount: NumberDecimal('5000.00'),
      type: 'Deposit',
      status: 'Completed',
      occurredAt: daysAgo(3),
      source: 'docker-seed',
    },
    {
      transactionId: UUID(),
      accountId: acc2,
      amount: NumberDecimal('120.00'),
      type: 'Withdrawal',
      status: 'Pending',
      occurredAt: daysAgo(2),
      source: 'docker-seed',
    },
    {
      transactionId: UUID(),
      accountId: acc3,
      amount: NumberDecimal('10000.00'),
      type: 'Deposit',
      status: 'Completed',
      occurredAt: daysAgo(1),
      source: 'docker-seed',
    },
    {
      transactionId: UUID(),
      accountId: acc3,
      amount: NumberDecimal('999.99'),
      type: 'Withdrawal',
      status: 'Failed',
      occurredAt: new Date(now.getTime() - 6 * 60 * 60 * 1000),
      source: 'docker-seed',
    },
  ]);

  historyDb[collName].createIndex({ accountId: 1, occurredAt: -1 });
  historyDb[collName].createIndex({ status: 1 });
  historyDb[collName].createIndex({ type: 1 });

  print(`Mongo seed [${dbName}.${collName}]: 6 documentos (docker-seed).`);
}
