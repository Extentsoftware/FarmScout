# FarmScout Lookup Data Summary

## Overview
This document provides a summary of the lookup data structure used in the FarmScout application. The data is organized in a hierarchical structure with Groups, SubGroups, and Items.

## Data Structure

### Groups (10 total)
Each group represents a major category of agricultural data:

1. **Crop Types** 🌾 - Different types of crops
2. **Diseases** 🦠 - Plant diseases and pathogens
3. **Pests** 🐛 - Agricultural pests and insects
4. **Chemicals** 🧪 - Agricultural chemicals and treatments
5. **Fertilizers** 🌱 - Fertilizers and soil amendments
6. **Soil Types** 🌍 - Different soil classifications
7. **Weather Conditions** 🌤️ - Weather and environmental conditions
8. **Growth Stages** 📈 - Plant growth and development stages
9. **Damage Types** 💥 - Types of crop damage
10. **Treatment Methods** 💊 - Methods for treating agricultural problems

### SubGroups
Most groups have subgroups that provide more specific categorization:

- **Chemicals**: Herbicide, Fungicide, Insecticide, Fertilizer, Growth Regulator, Other
- **Diseases**: Fungal, Bacterial, Viral, Nematode, Other
- **Pests**: Insects, Mites, Nematodes, Birds, Mammals
- **Fertilizers**: Compound, Straights, Micronutrients, Organic
- **Soil Types**: Mineral, Organic, Mixed
- **Weather Conditions**: Temperature, Precipitation, Wind, Humidity, Pressure
- **Growth Stages**: Vegetative, Reproductive, Maturity
- **Damage Types**: Environmental, Biological, Mechanical, Chemical
- **Treatment Methods**: Chemical, Biological, Cultural, Mechanical, Integrated

### Items
Individual lookup items with specific names and descriptions. Some examples:

- **Crop Types**: Corn, Soybeans, Wheat, Cotton, Rice
- **Diseases**: Rust, Blight, Mildew, Root Rot, Leaf Spot
- **Pests**: Aphids, Corn Borer, Spider Mites, Cutworms, Wireworms
- **Chemicals**: Glyphosate, Atrazine, 2,4-D, Paraquat, Dicamba
- **Fertilizers**: Urea, Ammonium Nitrate, Single Superphosphate, Potassium Chloride
- **Soil Types**: Clay, Silt, Sandy, Loam, Peat
- **Weather Conditions**: Sunny, Cloudy, Rainy, Windy, Foggy
- **Growth Stages**: Germination, Vegetative, Flowering, Fruiting, Maturity
- **Damage Types**: Hail Damage, Wind Damage, Drought Stress, Flood Damage, Frost Damage
- **Treatment Methods**: Chemical Treatment, Biological Control, Cultural Control, Mechanical Control, Integrated Pest Management

## CSV File Structure

The `lookup_data_spreadsheet.csv` file contains the following columns:

- **Group**: The main category (e.g., "Crop Types", "Diseases")
- **SubGroup**: The subcategory (e.g., "Fungal", "Herbicide") - may be empty for groups without subgroups
- **Item Name**: The specific item name (e.g., "Corn", "Rust", "Glyphosate")
- **Description**: Detailed description of the item
- **Icon**: Emoji icon representing the group
- **Color**: Hex color code for the group
- **Sort Order**: Numeric order for display purposes

## Usage

This lookup data is used throughout the FarmScout application to:
- Categorize observations and findings
- Provide standardized terminology for agricultural data
- Enable filtering and searching of observations
- Support data entry with predefined options
- Ensure consistency in agricultural terminology

## Data Source

This data is seeded into the application's SQLite database through the `DatabaseSeeder` class and represents common agricultural terms and categories used in farming and crop management. 