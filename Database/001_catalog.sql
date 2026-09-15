CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE IF NOT EXISTS "Categories" ("Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(), "Name" varchar(120) NOT NULL, "Slug" varchar(160) NOT NULL UNIQUE);
CREATE TABLE IF NOT EXISTS "Brands" ("Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(), "Name" varchar(120) NOT NULL, "Slug" varchar(160) NOT NULL UNIQUE);
CREATE TABLE IF NOT EXISTS "Products" ("Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(), "Name" varchar(160) NOT NULL, "Slug" varchar(160) NOT NULL UNIQUE, "Description" varchar(4000) NOT NULL DEFAULT '', "Price" numeric(12,2) NOT NULL, "IsFeatured" boolean NOT NULL DEFAULT false, "IsActive" boolean NOT NULL DEFAULT true, "CategoryId" uuid NOT NULL REFERENCES "Categories"("Id"), "BrandId" uuid NOT NULL REFERENCES "Brands"("Id"), "CreatedAtUtc" timestamptz NOT NULL DEFAULT now(), "UpdatedAtUtc" timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS "ProductVariants" ("Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(), "ProductId" uuid NOT NULL REFERENCES "Products"("Id") ON DELETE CASCADE, "Storage" varchar(40) NOT NULL, "Color" varchar(60) NOT NULL, "Stock" integer NOT NULL DEFAULT 0, "Price" numeric(12,2), UNIQUE("ProductId", "Storage", "Color"));
CREATE TABLE IF NOT EXISTS "ProductImages" ("Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(), "ProductId" uuid NOT NULL REFERENCES "Products"("Id") ON DELETE CASCADE, "CloudinaryUrl" text NOT NULL, "AltText" varchar(160), "SortOrder" integer NOT NULL DEFAULT 0, "IsPrimary" boolean NOT NULL DEFAULT false);

INSERT INTO "Categories" ("Name", "Slug") VALUES ('Smartphones', 'smartphones'), ('iPhone', 'iphone'), ('Android', 'android') ON CONFLICT ("Slug") DO NOTHING;
INSERT INTO "Brands" ("Name", "Slug") VALUES ('Apple', 'apple'), ('Samsung', 'samsung'), ('Xiaomi', 'xiaomi'), ('Motorola', 'motorola') ON CONFLICT ("Slug") DO NOTHING;
