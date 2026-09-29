-- Runs once, on the first start of an empty volume, as the bootstrap superuser.
-- Local-only credentials: production points the Reader/Writer connection strings elsewhere.

-- Owner role: owns both databases and every table the migrations create. Not a superuser.
CREATE ROLE cms_writer LOGIN PASSWORD 'cms_writer_local';
CREATE ROLE cms_reader LOGIN PASSWORD 'cms_reader_local';

CREATE DATABASE cms_api OWNER cms_writer;
CREATE DATABASE cms_api_test OWNER cms_writer;

\connect cms_api
ALTER SCHEMA public OWNER TO cms_writer;
GRANT CONNECT ON DATABASE cms_api TO cms_reader;
GRANT USAGE ON SCHEMA public TO cms_reader;
ALTER DEFAULT PRIVILEGES FOR ROLE cms_writer IN SCHEMA public GRANT SELECT ON TABLES TO cms_reader;

\connect cms_api_test
ALTER SCHEMA public OWNER TO cms_writer;
GRANT CONNECT ON DATABASE cms_api_test TO cms_reader;
GRANT USAGE ON SCHEMA public TO cms_reader;
ALTER DEFAULT PRIVILEGES FOR ROLE cms_writer IN SCHEMA public GRANT SELECT ON TABLES TO cms_reader;
