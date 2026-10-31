CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030204953_InitialCreate') THEN
    CREATE TABLE accounts (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Balance" numeric(18,2) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_accounts" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_accounts_balance_non_negative" CHECK ("Balance" >= 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030204953_InitialCreate') THEN
    CREATE TABLE transfers (
        "Id" uuid NOT NULL,
        "SourceAccountId" uuid NOT NULL,
        "DestinationAccountId" uuid NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "ReferenceId" character varying(100) NOT NULL,
        "Description" character varying(500) NOT NULL DEFAULT '',
        "Status" character varying(20) NOT NULL,
        "SourceTransactionId" uuid,
        "DestinationTransactionId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_transfers" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_transfers_amount_positive" CHECK ("Amount" > 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030204953_InitialCreate') THEN
    CREATE TABLE transactions (
        "Id" uuid NOT NULL,
        "AccountId" uuid NOT NULL,
        "Type" character varying(20) NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "Description" character varying(500) NOT NULL DEFAULT '',
        "ReferenceId" character varying(100) NOT NULL,
        "BalanceAfter" numeric(18,2) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_transactions" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_transactions_amount_positive" CHECK ("Amount" > 0),
        CONSTRAINT "FK_transactions_accounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES accounts ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030204953_InitialCreate') THEN
    CREATE INDEX "IX_transactions_account_created_at" ON transactions ("AccountId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030204953_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_transactions_account_reference_unique" ON transactions ("AccountId", "ReferenceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030204953_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_transfers_reference_unique" ON transfers ("ReferenceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261030204953_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261030204953_InitialCreate', '10.0.14');
    END IF;
END $EF$;
COMMIT;

