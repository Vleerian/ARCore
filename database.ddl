CREATE TABLE "regions" (
	"index"	INTEGER,
	"name"	TEXT NOT NULL UNIQUE,
	"nations"	TEXT,
	"numnations"	INTEGER,
	"delegate"	TEXT,
	"delegateauth"	TEXT,
	"founder"	TEXT,
	"factbook"	TEXT,
	"lastupdate"	INTEGER,
	"firstnation"	TEXT,
	"passworded"	INTEGER,
	"founderless"	INTEGER,
	PRIMARY KEY("index" AUTOINCREMENT)
)|||CREATE TABLE "nations" (
	"index"	INTEGER PRIMARY KEY AUTOINCREMENT,
	"name"	TEXT NOT NULL UNIQUE,
	"region"	TEXT NOT NULL,
	"endorsements"	INTEGER,
	"WAStatus"	INTEGER
);|||CREATE TABLE "endorsements" (
	"EndorserID"	INTEGER NOT NULL,
	"EndorseeID"	INTEGER NOT NULL
);|||CREATE TABLE "embassies" (
	"region1"	INTEGER NOT NULL,
	"region2"	INTEGER NOT NULL,
	UNIQUE (region1, region1) ON CONFLICT REPLACE
);