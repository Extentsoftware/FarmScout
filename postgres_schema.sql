-- FarmScout PostgreSQL Database Schema
-- This script creates the complete database schema for the FarmScout application

-- Enable PostGIS extension for spatial data
CREATE EXTENSION IF NOT EXISTS postgis;

-- Create UUID extension for GUID support
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create the database schema
CREATE SCHEMA IF NOT EXISTS farmscout;

-- Set search path
SET search_path TO farmscout, public;

-- ============================================================================
-- CORE TABLES
-- ============================================================================

-- Observations table
CREATE TABLE observations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    summary VARCHAR(500) NOT NULL,
    photo_path VARCHAR(1000),
    latitude DOUBLE PRECISION NOT NULL,
    longitude DOUBLE PRECISION NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    notes TEXT,
    severity VARCHAR(50) NOT NULL,
    farm_location_id UUID,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Farm Locations table with PostGIS geometry
CREATE TABLE farm_locations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    geometry GEOMETRY(POLYGON, 4326) NOT NULL,
    field_type VARCHAR(100),
    area DOUBLE PRECISION,
    owner VARCHAR(255),
    last_updated TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================================================
-- PHOTO TABLES
-- ============================================================================

-- Observation Photos table
CREATE TABLE observation_photos (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    observation_id UUID NOT NULL,
    photo_data BYTEA NOT NULL,
    mime_type VARCHAR(50) DEFAULT 'image/jpeg',
    original_file_name VARCHAR(255),
    file_size BIGINT NOT NULL,
    width INTEGER NOT NULL,
    height INTEGER NOT NULL,
    description TEXT,
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    is_active BOOLEAN DEFAULT TRUE,
    
    CONSTRAINT fk_observation_photos_observation 
        FOREIGN KEY (observation_id) REFERENCES observations(id) 
        ON DELETE CASCADE
);

-- ============================================================================
-- LOCATION TABLES
-- ============================================================================

-- Observation Locations table
CREATE TABLE observation_locations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    observation_id UUID NOT NULL,
    latitude DOUBLE PRECISION NOT NULL,
    longitude DOUBLE PRECISION NOT NULL,
    description TEXT,
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT fk_observation_locations_observation 
        FOREIGN KEY (observation_id) REFERENCES observations(id) 
        ON DELETE CASCADE
);

-- ============================================================================
-- TASK TABLES
-- ============================================================================

-- Task Items table
CREATE TABLE task_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    observation_id UUID NOT NULL,
    description TEXT NOT NULL,
    is_completed BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT fk_task_items_observation 
        FOREIGN KEY (observation_id) REFERENCES observations(id) 
        ON DELETE CASCADE
);

-- ============================================================================
-- LOOKUP TABLES
-- ============================================================================

-- Lookup Groups table
CREATE TABLE lookup_groups (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(50) UNIQUE NOT NULL,
    icon VARCHAR(10) DEFAULT '📝',
    color VARCHAR(7) DEFAULT '#607D8B',
    sort_order INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    is_active BOOLEAN DEFAULT TRUE
);

-- Lookup SubGroups table
CREATE TABLE lookup_sub_groups (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(50) NOT NULL,
    group_id UUID NOT NULL,
    sort_order INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    is_active BOOLEAN DEFAULT TRUE,
    
    CONSTRAINT fk_lookup_sub_groups_group 
        FOREIGN KEY (group_id) REFERENCES lookup_groups(id) 
        ON DELETE CASCADE
);

-- Lookup Items table
CREATE TABLE lookup_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    group_id UUID NOT NULL,
    sub_group_id UUID,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    is_active BOOLEAN DEFAULT TRUE,
    
    CONSTRAINT fk_lookup_items_group 
        FOREIGN KEY (group_id) REFERENCES lookup_groups(id) 
        ON DELETE CASCADE,
    CONSTRAINT fk_lookup_items_sub_group 
        FOREIGN KEY (sub_group_id) REFERENCES lookup_sub_groups(id) 
        ON DELETE SET NULL
);

-- ============================================================================
-- OBSERVATION TYPE TABLES
-- ============================================================================

-- Observation Types table
CREATE TABLE observation_types (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    icon VARCHAR(10),
    color VARCHAR(7) DEFAULT '#607D8B',
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    sort_order INTEGER DEFAULT 0
);

-- Observation Type Data Points table
CREATE TABLE observation_type_data_points (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    observation_type_id UUID NOT NULL,
    code VARCHAR(50) NOT NULL,
    label VARCHAR(100) NOT NULL,
    data_type VARCHAR(20) NOT NULL,
    lookup_group_name VARCHAR(100),
    description TEXT,
    is_required BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    sort_order INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT fk_observation_type_data_points_observation_type 
        FOREIGN KEY (observation_type_id) REFERENCES observation_types(id) 
        ON DELETE CASCADE
);

-- ============================================================================
-- METADATA TABLES
-- ============================================================================

-- Observation Metadata table
CREATE TABLE observation_metadata (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    observation_id UUID NOT NULL,
    observation_type_id UUID NOT NULL,
    data_point_id UUID NOT NULL,
    value TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT fk_observation_metadata_observation 
        FOREIGN KEY (observation_id) REFERENCES observations(id) 
        ON DELETE CASCADE,
    CONSTRAINT fk_observation_metadata_observation_type 
        FOREIGN KEY (observation_type_id) REFERENCES observation_types(id) 
        ON DELETE CASCADE,
    CONSTRAINT fk_observation_metadata_data_point 
        FOREIGN KEY (data_point_id) REFERENCES observation_type_data_points(id) 
        ON DELETE CASCADE
);

-- ============================================================================
-- INDEXES FOR PERFORMANCE
-- ============================================================================

-- Observations indexes
CREATE INDEX idx_observations_timestamp ON observations(timestamp DESC);
CREATE INDEX idx_observations_farm_location_id ON observations(farm_location_id);
CREATE INDEX idx_observations_severity ON observations(severity);
CREATE INDEX idx_observations_location ON observations USING GIST(ST_SetSRID(ST_MakePoint(longitude, latitude), 4326));

-- Farm Locations indexes
CREATE INDEX idx_farm_locations_geometry ON farm_locations USING GIST(geometry);
CREATE INDEX idx_farm_locations_name ON farm_locations(name);

-- Observation Photos indexes
CREATE INDEX idx_observation_photos_observation_id ON observation_photos(observation_id);
CREATE INDEX idx_observation_photos_timestamp ON observation_photos(timestamp);
CREATE INDEX idx_observation_photos_is_active ON observation_photos(is_active);

-- Observation Locations indexes
CREATE INDEX idx_observation_locations_observation_id ON observation_locations(observation_id);
CREATE INDEX idx_observation_locations_location ON observation_locations USING GIST(ST_SetSRID(ST_MakePoint(longitude, latitude), 4326));

-- Task Items indexes
CREATE INDEX idx_task_items_observation_id ON task_items(observation_id);
CREATE INDEX idx_task_items_is_completed ON task_items(is_completed);
CREATE INDEX idx_task_items_created_at ON task_items(created_at);

-- Lookup indexes
CREATE INDEX idx_lookup_groups_name ON lookup_groups(name);
CREATE INDEX idx_lookup_groups_is_active ON lookup_groups(is_active);
CREATE INDEX idx_lookup_groups_sort_order ON lookup_groups(sort_order);

CREATE INDEX idx_lookup_sub_groups_group_id ON lookup_sub_groups(group_id);
CREATE INDEX idx_lookup_sub_groups_is_active ON lookup_sub_groups(is_active);
CREATE INDEX idx_lookup_sub_groups_sort_order ON lookup_sub_groups(sort_order);

CREATE INDEX idx_lookup_items_group_id ON lookup_items(group_id);
CREATE INDEX idx_lookup_items_sub_group_id ON lookup_items(sub_group_id);
CREATE INDEX idx_lookup_items_name ON lookup_items(name);
CREATE INDEX idx_lookup_items_is_active ON lookup_items(is_active);

-- Observation Types indexes
CREATE INDEX idx_observation_types_name ON observation_types(name);
CREATE INDEX idx_observation_types_is_active ON observation_types(is_active);
CREATE INDEX idx_observation_types_sort_order ON observation_types(sort_order);

-- Observation Type Data Points indexes
CREATE INDEX idx_observation_type_data_points_observation_type_id ON observation_type_data_points(observation_type_id);
CREATE INDEX idx_observation_type_data_points_code ON observation_type_data_points(code);
CREATE INDEX idx_observation_type_data_points_is_active ON observation_type_data_points(is_active);
CREATE INDEX idx_observation_type_data_points_sort_order ON observation_type_data_points(sort_order);

-- Observation Metadata indexes
CREATE INDEX idx_observation_metadata_observation_id ON observation_metadata(observation_id);
CREATE INDEX idx_observation_metadata_observation_type_id ON observation_metadata(observation_type_id);
CREATE INDEX idx_observation_metadata_data_point_id ON observation_metadata(data_point_id);

-- ============================================================================
-- TRIGGERS FOR UPDATED_AT TIMESTAMPS
-- ============================================================================

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers for updated_at columns
CREATE TRIGGER update_observations_updated_at BEFORE UPDATE ON observations FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_farm_locations_updated_at BEFORE UPDATE ON farm_locations FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_observation_photos_updated_at BEFORE UPDATE ON observation_photos FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_lookup_groups_updated_at BEFORE UPDATE ON lookup_groups FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_lookup_sub_groups_updated_at BEFORE UPDATE ON lookup_sub_groups FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_lookup_items_updated_at BEFORE UPDATE ON lookup_items FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_observation_types_updated_at BEFORE UPDATE ON observation_types FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_observation_type_data_points_updated_at BEFORE UPDATE ON observation_type_data_points FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_observation_metadata_updated_at BEFORE UPDATE ON observation_metadata FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- VIEWS FOR COMMON QUERIES
-- ============================================================================

-- View for observations with photo count
CREATE VIEW observations_with_photo_count AS
SELECT 
    o.*,
    COUNT(op.id) as photo_count,
    ST_SetSRID(ST_MakePoint(o.longitude, o.latitude), 4326) as location_geom
FROM observations o
LEFT JOIN observation_photos op ON o.id = op.observation_id AND op.is_active = true
GROUP BY o.id;

-- View for observations with metadata
CREATE VIEW observations_with_metadata AS
SELECT 
    o.*,
    om.observation_type_id,
    om.data_point_id,
    om.value,
    ot.name as observation_type_name,
    dp.label as data_point_label
FROM observations o
LEFT JOIN observation_metadata om ON o.id = om.observation_id
LEFT JOIN observation_types ot ON om.observation_type_id = ot.id
LEFT JOIN observation_type_data_points dp ON om.data_point_id = dp.id;

-- ============================================================================
-- FUNCTIONS FOR SPATIAL QUERIES
-- ============================================================================

-- Function to find observations within a farm location
CREATE OR REPLACE FUNCTION get_observations_in_farm_location(farm_location_uuid UUID)
RETURNS TABLE (
    observation_id UUID,
    summary VARCHAR(500),
    latitude DOUBLE PRECISION,
    longitude DOUBLE PRECISION,
    timestamp TIMESTAMP WITH TIME ZONE
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        o.id,
        o.summary,
        o.latitude,
        o.longitude,
        o.timestamp
    FROM observations o
    JOIN farm_locations fl ON fl.id = farm_location_uuid
    WHERE ST_Contains(fl.geometry, ST_SetSRID(ST_MakePoint(o.longitude, o.latitude), 4326));
END;
$$ LANGUAGE plpgsql;

-- Function to find observations within a radius
CREATE OR REPLACE FUNCTION get_observations_within_radius(
    center_lat DOUBLE PRECISION,
    center_lon DOUBLE PRECISION,
    radius_meters DOUBLE PRECISION
)
RETURNS TABLE (
    observation_id UUID,
    summary VARCHAR(500),
    latitude DOUBLE PRECISION,
    longitude DOUBLE PRECISION,
    distance_meters DOUBLE PRECISION
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        o.id,
        o.summary,
        o.latitude,
        o.longitude,
        ST_Distance(
            ST_SetSRID(ST_MakePoint(center_lon, center_lat), 4326)::geography,
            ST_SetSRID(ST_MakePoint(o.longitude, o.latitude), 4326)::geography
        ) as distance_meters
    FROM observations o
    WHERE ST_DWithin(
        ST_SetSRID(ST_MakePoint(center_lon, center_lat), 4326)::geography,
        ST_SetSRID(ST_MakePoint(o.longitude, o.latitude), 4326)::geography,
        radius_meters
    )
    ORDER BY distance_meters;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- COMMENTS FOR DOCUMENTATION
-- ============================================================================

COMMENT ON SCHEMA farmscout IS 'FarmScout application database schema';
COMMENT ON TABLE observations IS 'Main observations table storing farm observations';
COMMENT ON TABLE farm_locations IS 'Farm field boundaries and locations using PostGIS geometry';
COMMENT ON TABLE observation_photos IS 'Photos associated with observations stored as bytea';
COMMENT ON TABLE observation_locations IS 'Additional location points for observations';
COMMENT ON TABLE task_items IS 'Tasks associated with observations';
COMMENT ON TABLE lookup_groups IS 'Categorization groups for lookup items';
COMMENT ON TABLE lookup_sub_groups IS 'Sub-categories within lookup groups';
COMMENT ON TABLE lookup_items IS 'Individual lookup items for categorization';
COMMENT ON TABLE observation_types IS 'Types of observations that can be made';
COMMENT ON TABLE observation_type_data_points IS 'Data points/fields for each observation type';
COMMENT ON TABLE observation_metadata IS 'Metadata values for observations';

-- ============================================================================
-- GRANTS (adjust as needed for your security requirements)
-- ============================================================================

-- Grant permissions to the farmscout schema
GRANT USAGE ON SCHEMA farmscout TO PUBLIC;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA farmscout TO PUBLIC;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA farmscout TO PUBLIC;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA farmscout TO PUBLIC;

-- ============================================================================
-- MIGRATION NOTES
-- ============================================================================

/*
Migration from SQLite to PostgreSQL:

1. UUIDs: SQLite uses GUID strings, PostgreSQL uses native UUID type
2. Timestamps: SQLite uses DateTime, PostgreSQL uses TIMESTAMP WITH TIME ZONE
3. Binary data: SQLite uses BLOB, PostgreSQL uses BYTEA
4. Geometry: SQLite uses WKT strings, PostgreSQL uses PostGIS geometry types
5. Indexes: PostgreSQL has more sophisticated indexing options including GIST for spatial data

Key differences:
- All foreign key constraints are explicitly defined
- Spatial indexes using GIST for PostGIS geometry columns
- Automatic updated_at timestamp triggers
- Better data type precision (DOUBLE PRECISION vs REAL)
- Native UUID support
- Full-text search capabilities available
- Better performance for complex spatial queries
*/
