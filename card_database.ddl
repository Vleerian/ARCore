CREATE TABLE "cards" (
    "index"         INTEGER,
	"season"	    INTEGER,
	"name"	        TEXT NOT NULL UNIQUE,
	"type"	        TEXT,
	"motto"	        TEXT,
	"category"	    TEXT,
	"region"    	TEXT,
	"cardcategory"	TEXT,
	PRIMARY KEY("index" AUTOINCREMENT)
);