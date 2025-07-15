-- FarmScout PostgreSQL Database Schema with PostGIS
-- This script creates the complete database schema for the FarmScout application

-- Enable PostGIS extension
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
    photo_path VARCHAR(1000), -- Legacy field for backward compatibility
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
    geometry GEOMETRY(POLYGON, 4326) NOT NULL, -- WGS84 coordinate system
    field_type VARCHAR(100),
    area DOUBLE PRECISION, -- in acres or hectares
    owner VARCHAR(255),
    last_updated TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================================================
-- PHOTO TABLES
-- ============================================================================

-- Observation Photos table (storing photos as bytea)
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
    data_type VARCHAR(20) NOT NULL, -- 'long', 'string', 'lookup'
    lookup_group_name VARCHAR(100), -- Only used when DataType is 'lookup'
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
-- SEED DATA INSERTION
-- ============================================================================

-- Insert all lookup groups from FarmScoutDatabase
INSERT INTO lookup_groups (name, icon, color, sort_order) VALUES
('Crop Types', '🌾', '#4CAF50', 1),
('Diseases', '🦠', '#F44336', 2),
('Pests', '🐛', '#FF9800', 3),
('Chemicals', '🧪', '#9C27B0', 4),
('Fertilizers', '🌱', '#8BC34A', 5),
('Soil Types', '🌍', '#8D6E63', 6),
('Weather Conditions', '🌤️', '#2196F3', 7),
('Growth Stages', '📈', '#00BCD4', 8),
('Damage Types', '💥', '#795548', 9),
('Treatment Methods', '💊', '#607D8B', 10);

-- Insert lookup subgroups
-- Chemicals subgroups
INSERT INTO lookup_sub_groups (name, group_id, sort_order) 
SELECT 'Herbicide', id, 1 FROM lookup_groups WHERE name = 'Chemicals'
UNION ALL
SELECT 'Fungicide', id, 2 FROM lookup_groups WHERE name = 'Chemicals'
UNION ALL
SELECT 'Insecticide', id, 3 FROM lookup_groups WHERE name = 'Chemicals'
UNION ALL
SELECT 'Fertilizer', id, 4 FROM lookup_groups WHERE name = 'Chemicals'
UNION ALL
SELECT 'Growth Regulator', id, 5 FROM lookup_groups WHERE name = 'Chemicals';

-- Diseases subgroups
INSERT INTO lookup_sub_groups (name, group_id, sort_order)
SELECT 'Fungal', id, 1 FROM lookup_groups WHERE name = 'Diseases'
UNION ALL
SELECT 'Bacterial', id, 2 FROM lookup_groups WHERE name = 'Diseases'
UNION ALL
SELECT 'Viral', id, 3 FROM lookup_groups WHERE name = 'Diseases'
UNION ALL
SELECT 'Nematode', id, 4 FROM lookup_groups WHERE name = 'Diseases'
UNION ALL
SELECT 'Other', id, 5 FROM lookup_groups WHERE name = 'Diseases';

-- Pests subgroups
INSERT INTO lookup_sub_groups (name, group_id, sort_order)
SELECT 'Insects', id, 1 FROM lookup_groups WHERE name = 'Pests'
UNION ALL
SELECT 'Mites', id, 2 FROM lookup_groups WHERE name = 'Pests'
UNION ALL
SELECT 'Nematodes', id, 3 FROM lookup_groups WHERE name = 'Pests'
UNION ALL
SELECT 'Birds', id, 4 FROM lookup_groups WHERE name = 'Pests'
UNION ALL
SELECT 'Mammals', id, 5 FROM lookup_groups WHERE name = 'Pests';

-- Fertilizers subgroups
INSERT INTO lookup_sub_groups (name, group_id, sort_order)
SELECT 'Nitrogen', id, 1 FROM lookup_groups WHERE name = 'Fertilizers'
UNION ALL
SELECT 'Phosphorus', id, 2 FROM lookup_groups WHERE name = 'Fertilizers'
UNION ALL
SELECT 'Potassium', id, 3 FROM lookup_groups WHERE name = 'Fertilizers'
UNION ALL
SELECT 'Micronutrients', id, 4 FROM lookup_groups WHERE name = 'Fertilizers'
UNION ALL
SELECT 'Organic', id, 5 FROM lookup_groups WHERE name = 'Fertilizers';

-- Soil Types subgroups
INSERT INTO lookup_sub_groups (name, group_id, sort_order)
SELECT 'Mineral', id, 1 FROM lookup_groups WHERE name = 'Soil Types'
UNION ALL
SELECT 'Organic', id, 2 FROM lookup_groups WHERE name = 'Soil Types'
UNION ALL
SELECT 'Mixed', id, 3 FROM lookup_groups WHERE name = 'Soil Types';

-- Weather Conditions subgroups
INSERT INTO lookup_sub_groups (name, group_id, sort_order)
SELECT 'Temperature', id, 1 FROM lookup_groups WHERE name = 'Weather Conditions'
UNION ALL
SELECT 'Precipitation', id, 2 FROM lookup_groups WHERE name = 'Weather Conditions'
UNION ALL
SELECT 'Wind', id, 3 FROM lookup_groups WHERE name = 'Weather Conditions'
UNION ALL
SELECT 'Humidity', id, 4 FROM lookup_groups WHERE name = 'Weather Conditions'
UNION ALL
SELECT 'Pressure', id, 5 FROM lookup_groups WHERE name = 'Weather Conditions';

-- Growth Stages subgroups
INSERT INTO lookup_sub_groups (name, group_id, sort_order)
SELECT 'Vegetative', id, 1 FROM lookup_groups WHERE name = 'Growth Stages'
UNION ALL
SELECT 'Reproductive', id, 2 FROM lookup_groups WHERE name = 'Growth Stages'
UNION ALL
SELECT 'Maturity', id, 3 FROM lookup_groups WHERE name = 'Growth Stages';

-- Damage Types subgroups
INSERT INTO lookup_sub_groups (name, group_id, sort_order)
SELECT 'Environmental', id, 1 FROM lookup_groups WHERE name = 'Damage Types'
UNION ALL
SELECT 'Biological', id, 2 FROM lookup_groups WHERE name = 'Damage Types'
UNION ALL
SELECT 'Mechanical', id, 3 FROM lookup_groups WHERE name = 'Damage Types'
UNION ALL
SELECT 'Chemical', id, 4 FROM lookup_groups WHERE name = 'Damage Types';

-- Treatment Methods subgroups
INSERT INTO lookup_sub_groups (name, group_id, sort_order)
SELECT 'Chemical', id, 1 FROM lookup_groups WHERE name = 'Treatment Methods'
UNION ALL
SELECT 'Biological', id, 2 FROM lookup_groups WHERE name = 'Treatment Methods'
UNION ALL
SELECT 'Cultural', id, 3 FROM lookup_groups WHERE name = 'Treatment Methods'
UNION ALL
SELECT 'Mechanical', id, 4 FROM lookup_groups WHERE name = 'Treatment Methods'
UNION ALL
SELECT 'Integrated', id, 5 FROM lookup_groups WHERE name = 'Treatment Methods';

-- Insert lookup items
-- Crop Types (no subgroups)
INSERT INTO lookup_items (name, group_id, description)
SELECT 'Corn', id, 'Maize crop for grain or silage' FROM lookup_groups WHERE name = 'Crop Types'
UNION ALL
SELECT 'Soybeans', id, 'Legume crop for oil and protein' FROM lookup_groups WHERE name = 'Crop Types'
UNION ALL
SELECT 'Wheat', id, 'Cereal grain crop' FROM lookup_groups WHERE name = 'Crop Types'
UNION ALL
SELECT 'Cotton', id, 'Fiber crop' FROM lookup_groups WHERE name = 'Crop Types'
UNION ALL
SELECT 'Rice', id, 'Staple grain crop' FROM lookup_groups WHERE name = 'Crop Types';

-- Diseases
INSERT INTO lookup_items (name, group_id, sub_group_id, description)
SELECT 'Rust', lg.id, lsg.id, 'Fungal disease affecting leaves and stems'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Fungal'
WHERE lg.name = 'Diseases'
UNION ALL
SELECT 'Blight', lg.id, lsg.id, 'Rapid plant disease causing wilting'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Bacterial'
WHERE lg.name = 'Diseases'
UNION ALL
SELECT 'Mildew', lg.id, lsg.id, 'Fungal growth on plant surfaces'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Fungal'
WHERE lg.name = 'Diseases'
UNION ALL
SELECT 'Root Rot', lg.id, lsg.id, 'Fungal disease affecting plant roots'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Fungal'
WHERE lg.name = 'Diseases'
UNION ALL
SELECT 'Leaf Spot', lg.id, lsg.id, 'Fungal disease causing spots on leaves'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Fungal'
WHERE lg.name = 'Diseases';

-- Pests
INSERT INTO lookup_items (name, group_id, sub_group_id, description)
SELECT 'Aphids', lg.id, lsg.id, 'Small sap-sucking insects'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Insects'
WHERE lg.name = 'Pests'
UNION ALL
SELECT 'Corn Borer', lg.id, lsg.id, 'Larva that bores into corn stalks'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Insects'
WHERE lg.name = 'Pests'
UNION ALL
SELECT 'Spider Mites', lg.id, lsg.id, 'Tiny arachnids that feed on plant sap'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Mites'
WHERE lg.name = 'Pests'
UNION ALL
SELECT 'Cutworms', lg.id, lsg.id, 'Caterpillars that cut plant stems'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Insects'
WHERE lg.name = 'Pests'
UNION ALL
SELECT 'Wireworms', lg.id, lsg.id, 'Click beetle larvae that damage roots'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Insects'
WHERE lg.name = 'Pests';

-- Chemicals
INSERT INTO lookup_items (name, group_id, sub_group_id, description)
SELECT 'Glyphosate', lg.id, lsg.id, 'Broad-spectrum herbicide'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Herbicide'
WHERE lg.name = 'Chemicals'
UNION ALL
SELECT 'Atrazine', lg.id, lsg.id, 'Selective herbicide for corn'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Herbicide'
WHERE lg.name = 'Chemicals'
UNION ALL
SELECT '2,4-D', lg.id, lsg.id, 'Selective herbicide for broadleaf weeds'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Herbicide'
WHERE lg.name = 'Chemicals'
UNION ALL
SELECT 'Paraquat', lg.id, lsg.id, 'Contact herbicide'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Herbicide'
WHERE lg.name = 'Chemicals'
UNION ALL
SELECT 'Dicamba', lg.id, lsg.id, 'Selective herbicide for broadleaf weeds'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Herbicide'
WHERE lg.name = 'Chemicals'
UNION ALL
SELECT 'Chlorothalonil', lg.id, lsg.id, 'Protectant fungicide for foliar diseases'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Fungicide'
WHERE lg.name = 'Chemicals'
UNION ALL
SELECT 'Azoxystrobin', lg.id, lsg.id, 'Systemic fungicide for disease control'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Fungicide'
WHERE lg.name = 'Chemicals'
UNION ALL
SELECT 'Malathion', lg.id, lsg.id, 'Organophosphate insecticide'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Insecticide'
WHERE lg.name = 'Chemicals'
UNION ALL
SELECT 'Carbaryl', lg.id, lsg.id, 'Carbamate insecticide for pest control'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Insecticide'
WHERE lg.name = 'Chemicals'
UNION ALL
SELECT 'Gibberellic Acid', lg.id, lsg.id, 'Plant growth regulator'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Growth Regulator'
WHERE lg.name = 'Chemicals';

-- Fertilizers
INSERT INTO lookup_items (name, group_id, sub_group_id, description)
SELECT 'Urea', lg.id, lsg.id, 'Nitrogen fertilizer (46-0-0)'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Nitrogen'
WHERE lg.name = 'Fertilizers'
UNION ALL
SELECT 'Ammonium Nitrate', lg.id, lsg.id, 'Nitrogen fertilizer (34-0-0)'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Nitrogen'
WHERE lg.name = 'Fertilizers'
UNION ALL
SELECT 'Triple Superphosphate', lg.id, lsg.id, 'Phosphorus fertilizer (0-46-0)'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Phosphorus'
WHERE lg.name = 'Fertilizers'
UNION ALL
SELECT 'Potassium Chloride', lg.id, lsg.id, 'Potassium fertilizer (0-0-60)'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Potassium'
WHERE lg.name = 'Fertilizers'
UNION ALL
SELECT 'NPK 10-10-10', lg.id, lsg.id, 'Balanced fertilizer'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Nitrogen'
WHERE lg.name = 'Fertilizers'
UNION ALL
SELECT 'Compost', lg.id, lsg.id, 'Organic soil amendment'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Organic'
WHERE lg.name = 'Fertilizers';

-- Soil Types
INSERT INTO lookup_items (name, group_id, sub_group_id, description)
SELECT 'Clay', lg.id, lsg.id, 'Fine-grained soil with high water retention'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Mineral'
WHERE lg.name = 'Soil Types'
UNION ALL
SELECT 'Silt', lg.id, lsg.id, 'Medium-grained soil'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Mineral'
WHERE lg.name = 'Soil Types'
UNION ALL
SELECT 'Sandy', lg.id, lsg.id, 'Coarse-grained soil with good drainage'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Mineral'
WHERE lg.name = 'Soil Types'
UNION ALL
SELECT 'Loam', lg.id, lsg.id, 'Well-balanced soil mixture'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Mixed'
WHERE lg.name = 'Soil Types'
UNION ALL
SELECT 'Peat', lg.id, lsg.id, 'Organic-rich soil'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Organic'
WHERE lg.name = 'Soil Types';

-- Weather Conditions
INSERT INTO lookup_items (name, group_id, sub_group_id, description)
SELECT 'Sunny', lg.id, lsg.id, 'Clear skies with full sun'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Temperature'
WHERE lg.name = 'Weather Conditions'
UNION ALL
SELECT 'Cloudy', lg.id, lsg.id, 'Overcast conditions'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Pressure'
WHERE lg.name = 'Weather Conditions'
UNION ALL
SELECT 'Rainy', lg.id, lsg.id, 'Precipitation occurring'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Precipitation'
WHERE lg.name = 'Weather Conditions'
UNION ALL
SELECT 'Windy', lg.id, lsg.id, 'High wind conditions'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Wind'
WHERE lg.name = 'Weather Conditions'
UNION ALL
SELECT 'Foggy', lg.id, lsg.id, 'Low visibility due to fog'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Humidity'
WHERE lg.name = 'Weather Conditions';

-- Growth Stages
INSERT INTO lookup_items (name, group_id, sub_group_id, description)
SELECT 'Germination', lg.id, lsg.id, 'Seed sprouting and root development'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Vegetative'
WHERE lg.name = 'Growth Stages'
UNION ALL
SELECT 'Vegetative', lg.id, lsg.id, 'Leaf and stem growth'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Vegetative'
WHERE lg.name = 'Growth Stages'
UNION ALL
SELECT 'Flowering', lg.id, lsg.id, 'Flower development and pollination'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Reproductive'
WHERE lg.name = 'Growth Stages'
UNION ALL
SELECT 'Fruiting', lg.id, lsg.id, 'Fruit or grain development'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Reproductive'
WHERE lg.name = 'Growth Stages'
UNION ALL
SELECT 'Maturity', lg.id, lsg.id, 'Full development and harvest ready'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Maturity'
WHERE lg.name = 'Growth Stages';

-- Damage Types
INSERT INTO lookup_items (name, group_id, sub_group_id, description)
SELECT 'Hail Damage', lg.id, lsg.id, 'Physical damage from hail stones'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Environmental'
WHERE lg.name = 'Damage Types'
UNION ALL
SELECT 'Wind Damage', lg.id, lsg.id, 'Damage from high winds'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Environmental'
WHERE lg.name = 'Damage Types'
UNION ALL
SELECT 'Drought Stress', lg.id, lsg.id, 'Damage from lack of water'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Environmental'
WHERE lg.name = 'Damage Types'
UNION ALL
SELECT 'Flood Damage', lg.id, lsg.id, 'Damage from excess water'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Environmental'
WHERE lg.name = 'Damage Types'
UNION ALL
SELECT 'Frost Damage', lg.id, lsg.id, 'Damage from freezing temperatures'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Environmental'
WHERE lg.name = 'Damage Types';

-- Treatment Methods
INSERT INTO lookup_items (name, group_id, sub_group_id, description)
SELECT 'Chemical Treatment', lg.id, lsg.id, 'Application of pesticides or herbicides'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Chemical'
WHERE lg.name = 'Treatment Methods'
UNION ALL
SELECT 'Biological Control', lg.id, lsg.id, 'Use of natural predators or beneficial organisms'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Biological'
WHERE lg.name = 'Treatment Methods'
UNION ALL
SELECT 'Cultural Control', lg.id, lsg.id, 'Management practices to prevent problems'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Cultural'
WHERE lg.name = 'Treatment Methods'
UNION ALL
SELECT 'Mechanical Control', lg.id, lsg.id, 'Physical removal or barriers'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Mechanical'
WHERE lg.name = 'Treatment Methods'
UNION ALL
SELECT 'Integrated Pest Management', lg.id, lsg.id, 'Combined approach using multiple methods'
FROM lookup_groups lg
LEFT JOIN lookup_sub_groups lsg ON lg.id = lsg.group_id AND lsg.name = 'Integrated'
WHERE lg.name = 'Treatment Methods';

-- Insert all observation types from FarmScoutDatabase
INSERT INTO observation_types (name, description, icon, color, sort_order) VALUES
('Disease', 'Plant disease observations', '🦠', '#F44336', 1),
('Dead Plant', 'Dead or dying plant observations', '💀', '#9E9E9E', 2),
('Pest', 'Pest infestation observations', '🐛', '#FF9800', 3),
('Damage', 'Plant damage observations', '💥', '#795548', 4),
('Growth', 'Plant growth observations', '🌱', '#4CAF50', 5),
('Harvest', 'Harvest observations', '🌾', '#FFC107', 6),
('Weather', 'Weather condition observations', '🌤️', '#2196F3', 7),
('Soil', 'Soil condition observations', '🌍', '#8D6E63', 8),
('Soil Moisture', 'Soil moisture observations', '💧', '#00BCD4', 9);

-- Insert observation type data points
-- Disease data points
INSERT INTO observation_type_data_points (observation_type_id, code, label, data_type, lookup_group_name, is_required, sort_order)
SELECT id, 'disease_name', 'Disease Name', 'lookup', 'Diseases', true, 1 FROM observation_types WHERE name = 'Disease'
UNION ALL
SELECT id, 'affected_area', 'Affected Area %', 'long', NULL, false, 2 FROM observation_types WHERE name = 'Disease'
UNION ALL
SELECT id, 'plant_count', 'Plant Count', 'long', NULL, false, 3 FROM observation_types WHERE name = 'Disease'
UNION ALL
SELECT id, 'symptoms', 'Symptoms', 'string', NULL, false, 4 FROM observation_types WHERE name = 'Disease';

-- Pest data points
INSERT INTO observation_type_data_points (observation_type_id, code, label, data_type, lookup_group_name, is_required, sort_order)
SELECT id, 'pest_name', 'Pest Name', 'lookup', 'Pests', true, 1 FROM observation_types WHERE name = 'Pest'
UNION ALL
SELECT id, 'pest_count', 'Pest Count', 'long', NULL, false, 2 FROM observation_types WHERE name = 'Pest'
UNION ALL
SELECT id, 'damage_level', 'Damage Level', 'long', NULL, false, 3 FROM observation_types WHERE name = 'Pest'
UNION ALL
SELECT id, 'infestation_area', 'Infestation Area', 'string', NULL, false, 4 FROM observation_types WHERE name = 'Pest';

-- Harvest data points
INSERT INTO observation_type_data_points (observation_type_id, code, label, data_type, lookup_group_name, is_required, sort_order)
SELECT id, 'crop_type', 'Crop Type', 'lookup', 'Crop Types', true, 1 FROM observation_types WHERE name = 'Harvest'
UNION ALL
SELECT id, 'weight_kg', 'Weight (kg)', 'long', NULL, false, 2 FROM observation_types WHERE name = 'Harvest'
UNION ALL
SELECT id, 'quality', 'Quality', 'string', NULL, false, 3 FROM observation_types WHERE name = 'Harvest'
UNION ALL
SELECT id, 'yield_per_area', 'Yield per Area', 'long', NULL, false, 4 FROM observation_types WHERE name = 'Harvest';

-- Weather data points
INSERT INTO observation_type_data_points (observation_type_id, code, label, data_type, lookup_group_name, is_required, sort_order)
SELECT id, 'temperature', 'Temperature (°C)', 'long', NULL, false, 1 FROM observation_types WHERE name = 'Weather'
UNION ALL
SELECT id, 'humidity', 'Humidity (%)', 'long', NULL, false, 2 FROM observation_types WHERE name = 'Weather'
UNION ALL
SELECT id, 'wind_speed', 'Wind Speed', 'long', NULL, false, 3 FROM observation_types WHERE name = 'Weather'
UNION ALL
SELECT id, 'precipitation', 'Precipitation', 'long', NULL, false, 4 FROM observation_types WHERE name = 'Weather';

-- Soil data points
INSERT INTO observation_type_data_points (observation_type_id, code, label, data_type, lookup_group_name, is_required, sort_order)
SELECT id, 'ph_level', 'pH Level', 'long', NULL, false, 1 FROM observation_types WHERE name = 'Soil'
UNION ALL
SELECT id, 'moisture', 'Moisture %', 'long', NULL, false, 2 FROM observation_types WHERE name = 'Soil'
UNION ALL
SELECT id, 'temperature', 'Temperature (°C)', 'long', NULL, false, 3 FROM observation_types WHERE name = 'Soil'
UNION ALL
SELECT id, 'nutrient_level', 'Nutrient Level', 'long', NULL, false, 4 FROM observation_types WHERE name = 'Soil';

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