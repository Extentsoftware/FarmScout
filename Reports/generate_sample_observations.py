import pandas as pd
import random
from datetime import datetime, timedelta
import numpy as np

def generate_sample_observations():
    """Generate about 200 sample observations across all observation types"""
    
    # Define the data structure
    sections = ['B1S1', 'B1S2', 'B2S1A', 'B2S1B', 'B2S2', 'B2S3', 'B2S4', 'B2S5', 'B2S6', 'B2S7', 'B2S8']
    
    # Existing metrics from CSV
    existing_metrics = [
        'Insect pests', 'basin formation', 'basin weeds', 'basins', 'bud break', 'contours', 
        'dead plants', 'destumping', 'diseases', 'drippers', 'fire guard', 'flowering', 
        'health status', 'inaccessibles', 'inrows', 'insect pests', 'leaf colour', 'moisture', 
        'mowing', 'nutdrop', 'nutsetting', 'pests', 'pipes', 'plant growth', 'pruning', 
        'remaining nuts', 'soil moisture', 'soil nutrients', 'staking'
    ]
    
    # New observation types from MAUI app
    maui_observation_types = [
        'Disease', 'Dead Plant', 'Pest', 'Damage', 'Growth', 'Harvest', 'Weather', 'Soil', 'Soil Moisture'
    ]
    
    # Combine all metrics
    all_metrics = existing_metrics + maui_observation_types
    
    conditions = ['pass', 'partial', 'fail']
    
    # Sample notes for different conditions
    sample_notes = {
        'pass': [
            'All systems operating normally',
            'No issues detected',
            'Good condition maintained',
            'Standards met',
            'Healthy growth observed',
            'Optimal conditions',
            'No intervention needed',
            'Excellent performance'
        ],
        'partial': [
            'Some areas need attention',
            'Minor issues detected',
            'Requires monitoring',
            'Partial completion',
            'Mixed results observed',
            'Some improvement needed',
            'In progress',
            'Moderate condition'
        ],
        'fail': [
            'Immediate action required',
            'Critical issues detected',
            'Standards not met',
            'Intervention needed',
            'Poor condition',
            'Requires immediate attention',
            'Failed inspection',
            'Urgent action required'
        ]
    }
    
    # Specific notes for different metric types
    metric_specific_notes = {
        'Insect pests': {
            'pass': ['No pests detected', 'Pest control effective', 'Clean inspection'],
            'partial': ['Few pests observed', 'Minor infestation', 'Some pest activity'],
            'fail': ['Heavy infestation', 'Pest damage visible', 'Control measures needed']
        },
        'diseases': {
            'pass': ['No disease symptoms', 'Healthy plants', 'Disease-free area'],
            'partial': ['Some disease symptoms', 'Minor infection', 'Monitoring required'],
            'fail': ['Severe disease outbreak', 'Multiple infected plants', 'Treatment needed']
        },
        'moisture': {
            'pass': ['Optimal moisture levels', 'Well-irrigated', 'Good soil moisture'],
            'partial': ['Some areas dry', 'Irregular moisture', 'Partial irrigation'],
            'fail': ['Severe drought stress', 'Poor irrigation', 'Dry conditions']
        },
        'Disease': {
            'pass': ['No disease detected', 'Healthy crop', 'Disease-free zone'],
            'partial': ['Early disease symptoms', 'Minor infection', 'Preventive measures needed'],
            'fail': ['Disease outbreak', 'Multiple infected plants', 'Treatment required']
        },
        'Pest': {
            'pass': ['No pest activity', 'Pest-free area', 'Good pest management'],
            'partial': ['Low pest pressure', 'Some pest activity', 'Monitoring needed'],
            'fail': ['High pest pressure', 'Pest damage evident', 'Control required']
        },
        'Weather': {
            'pass': ['Favorable conditions', 'Good weather', 'Optimal climate'],
            'partial': ['Mixed conditions', 'Some weather concerns', 'Variable weather'],
            'fail': ['Adverse weather', 'Weather damage', 'Poor conditions']
        },
        'Soil': {
            'pass': ['Good soil condition', 'Optimal soil health', 'Fertile soil'],
            'partial': ['Some soil issues', 'Mixed soil quality', 'Soil improvement needed'],
            'fail': ['Poor soil condition', 'Soil degradation', 'Soil treatment required']
        }
    }
    
    # Generate dates (last 6 months)
    end_date = datetime.now()
    start_date = end_date - timedelta(days=180)
    date_range = pd.date_range(start=start_date, end=end_date, freq='D')
    
    observations = []
    observation_id = 1
    
    # Generate observations
    for _ in range(200):
        # Random date
        date = random.choice(date_range)
        
        # Random section
        section = random.choice(sections)
        
        # Random metric (weighted towards existing metrics for realism)
        if random.random() < 0.7:  # 70% existing metrics
            metric = random.choice(existing_metrics)
        else:  # 30% new MAUI observation types
            metric = random.choice(maui_observation_types)
        
        # Random condition with realistic distribution
        condition_weights = [0.6, 0.3, 0.1]  # 60% pass, 30% partial, 10% fail
        condition = random.choices(conditions, weights=condition_weights)[0]
        
        # Generate appropriate notes
        if metric in metric_specific_notes:
            notes = random.choice(metric_specific_notes[metric][condition])
        else:
            notes = random.choice(sample_notes[condition])
        
        # Add some variety to notes
        if random.random() < 0.3:  # 30% chance to add additional details
            additional_details = [
                f" - {random.randint(1, 10)} plants affected",
                f" - {random.randint(5, 50)}% area coverage",
                f" - {random.choice(['North', 'South', 'East', 'West'])} section",
                f" - {random.choice(['Morning', 'Afternoon', 'Evening'])} inspection",
                f" - {random.randint(1, 100)}% severity",
                f" - {random.choice(['Low', 'Medium', 'High'])} priority"
            ]
            notes += random.choice(additional_details)
        
        # Create observation record
        observation = {
            'id': observation_id,
            'date': date.strftime('%d/%m/%Y'),
            'section': section,
            'metric': metric,
            'condition': condition,
            'notes': notes
        }
        
        observations.append(observation)
        observation_id += 1
    
    # Create DataFrame
    df = pd.DataFrame(observations)
    
    # Sort by date
    df = df.sort_values('date')
    
    # Save to CSV
    output_file = 'sample_observations.csv'
    df.to_csv(output_file, index=False)
    
    print(f"Generated {len(observations)} sample observations")
    print(f"Saved to {output_file}")
    
    # Print summary statistics
    print("\nSummary Statistics:")
    print(f"Date range: {df['date'].min()} to {df['date'].max()}")
    print(f"Total observations: {len(df)}")
    print(f"Unique sections: {df['section'].nunique()}")
    print(f"Unique metrics: {df['metric'].nunique()}")
    
    print("\nSection distribution:")
    section_counts = df['section'].value_counts().sort_index()
    for section, count in section_counts.items():
        print(f"  {section}: {count}")
    
    print("\nMetric distribution (top 10):")
    metric_counts = df['metric'].value_counts().head(10)
    for metric, count in metric_counts.items():
        print(f"  {metric}: {count}")
    
    print("\nCondition distribution:")
    condition_counts = df['condition'].value_counts()
    for condition, count in condition_counts.items():
        print(f"  {condition}: {count}")
    
    return df

if __name__ == "__main__":
    generate_sample_observations() 