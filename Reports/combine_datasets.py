import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
from datetime import datetime

def combine_datasets():
    """Combine original scout.csv data with new sample observations"""
    
    # Read both datasets
    original_df = pd.read_csv('scout.csv')
    sample_df = pd.read_csv('sample_observations.csv')
    
    # Parse dates
    original_df['date'] = pd.to_datetime(original_df['date'], format='%d/%m/%Y')
    sample_df['date'] = pd.to_datetime(sample_df['date'], format='%d/%m/%Y')
    
    # Add source column to distinguish datasets
    original_df['source'] = 'Original'
    sample_df['source'] = 'Sample'
    
    # Combine datasets
    combined_df = pd.concat([original_df, sample_df], ignore_index=True)
    
    # Sort by date
    combined_df = combined_df.sort_values('date')
    
    # Save combined dataset
    combined_df.to_csv('combined_observations.csv', index=False)
    
    print(f"Combined dataset created:")
    print(f"Original observations: {len(original_df)}")
    print(f"Sample observations: {len(sample_df)}")
    print(f"Total combined: {len(combined_df)}")
    print(f"Saved to: combined_observations.csv")
    
    # Create comparison dashboard
    create_comparison_dashboard(original_df, sample_df, combined_df)
    
    return combined_df

def create_comparison_dashboard(original_df, sample_df, combined_df):
    """Create a dashboard comparing original and sample datasets"""
    
    fig = plt.figure(figsize=(20, 16))
    gs = fig.add_gridspec(4, 3, hspace=0.4, wspace=0.3)
    
    # 1. Dataset Size Comparison
    ax1 = fig.add_subplot(gs[0, 0])
    sizes = [len(original_df), len(sample_df)]
    labels = ['Original', 'Sample']
    colors = ['#FF6B6B', '#4ECDC4']
    wedges, texts, autotexts = ax1.pie(sizes, labels=labels, autopct='%1.1f%%', 
                                       colors=colors, startangle=90)
    ax1.set_title('Dataset Size Comparison', fontsize=12, fontweight='bold')
    
    # 2. Date Range Comparison
    ax2 = fig.add_subplot(gs[0, 1])
    date_ranges = [
        f"Original: {original_df['date'].min().strftime('%Y-%m-%d')} to {original_df['date'].max().strftime('%Y-%m-%d')}",
        f"Sample: {sample_df['date'].min().strftime('%Y-%m-%d')} to {sample_df['date'].max().strftime('%Y-%m-%d')}"
    ]
    ax2.text(0.1, 0.8, '\n'.join(date_ranges), transform=ax2.transAxes, fontsize=10,
             bbox=dict(boxstyle='round', facecolor='lightblue', alpha=0.8))
    ax2.set_title('Date Range Comparison', fontsize=12, fontweight='bold')
    ax2.axis('off')
    
    # 3. Metric Coverage Comparison
    ax3 = fig.add_subplot(gs[0, 2])
    original_metrics = set(original_df['metric'].unique())
    sample_metrics = set(sample_df['metric'].unique())
    combined_metrics = original_metrics.union(sample_metrics)
    
    coverage_data = [
        len(original_metrics),
        len(sample_metrics),
        len(original_metrics.intersection(sample_metrics)),
        len(combined_metrics)
    ]
    coverage_labels = ['Original Only', 'Sample Only', 'Both', 'Total Unique']
    colors_coverage = ['#FF6B6B', '#4ECDC4', '#45B7D1', '#96CEB4']
    
    wedges, texts, autotexts = ax3.pie(coverage_data, labels=coverage_labels, autopct='%1.1f%%', 
                                       colors=colors_coverage, startangle=90)
    ax3.set_title('Metric Coverage', fontsize=12, fontweight='bold')
    
    # 4. Condition Distribution Comparison
    ax4 = fig.add_subplot(gs[1, 0])
    original_conditions = original_df['condition'].value_counts()
    sample_conditions = sample_df['condition'].value_counts()
    
    x = range(len(original_conditions))
    width = 0.35
    
    ax4.bar([i - width/2 for i in x], original_conditions.values, width, label='Original', color='#FF6B6B', alpha=0.7)
    ax4.bar([i + width/2 for i in x], sample_conditions.values, width, label='Sample', color='#4ECDC4', alpha=0.7)
    
    ax4.set_xlabel('Condition')
    ax4.set_ylabel('Count')
    ax4.set_title('Condition Distribution Comparison', fontsize=12, fontweight='bold')
    ax4.set_xticks(x)
    ax4.set_xticklabels(original_conditions.index)
    ax4.legend()
    
    # 5. Section Distribution Comparison
    ax5 = fig.add_subplot(gs[1, 1:])
    original_sections = original_df['section'].value_counts()
    sample_sections = sample_df['section'].value_counts()
    
    # Get all unique sections
    all_sections = sorted(set(original_sections.index) | set(sample_sections.index))
    
    x = range(len(all_sections))
    width = 0.35
    
    original_counts = [original_sections.get(section, 0) for section in all_sections]
    sample_counts = [sample_sections.get(section, 0) for section in all_sections]
    
    ax5.bar([i - width/2 for i in x], original_counts, width, label='Original', color='#FF6B6B', alpha=0.7)
    ax5.bar([i + width/2 for i in x], sample_counts, width, label='Sample', color='#4ECDC4', alpha=0.7)
    
    ax5.set_xlabel('Section')
    ax5.set_ylabel('Count')
    ax5.set_title('Section Distribution Comparison', fontsize=12, fontweight='bold')
    ax5.set_xticks(x)
    ax5.set_xticklabels(all_sections, rotation=45, ha='right')
    ax5.legend()
    
    # 6. Timeline Comparison
    ax6 = fig.add_subplot(gs[2, :2])
    
    # Monthly aggregation
    original_monthly = original_df.groupby(original_df['date'].dt.to_period('M')).size()
    sample_monthly = sample_df.groupby(sample_df['date'].dt.to_period('M')).size()
    
    ax6.plot(original_monthly.index.astype(str), original_monthly.values, 
             marker='o', linewidth=2, label='Original', color='#FF6B6B')
    ax6.plot(sample_monthly.index.astype(str), sample_monthly.values, 
             marker='s', linewidth=2, label='Sample', color='#4ECDC4')
    
    ax6.set_xlabel('Month')
    ax6.set_ylabel('Observations')
    ax6.set_title('Monthly Observation Count Comparison', fontsize=12, fontweight='bold')
    ax6.legend()
    ax6.grid(True, alpha=0.3)
    plt.setp(ax6.xaxis.get_majorticklabels(), rotation=45)
    
    # 7. MAUI Observation Types Analysis
    ax7 = fig.add_subplot(gs[2, 2])
    
    maui_types = ['Disease', 'Dead Plant', 'Pest', 'Damage', 'Growth', 'Harvest', 'Weather', 'Soil', 'Soil Moisture']
    
    # Count MAUI types in each dataset
    original_maui = original_df[original_df['metric'].isin(maui_types)].shape[0]
    sample_maui = sample_df[sample_df['metric'].isin(maui_types)].shape[0]
    
    maui_data = [original_maui, sample_maui]
    maui_labels = ['Original', 'Sample']
    
    wedges, texts, autotexts = ax7.pie(maui_data, labels=maui_labels, autopct='%1.1f%%', 
                                       colors=['#FF6B6B', '#4ECDC4'], startangle=90)
    ax7.set_title('MAUI Observation Types', fontsize=12, fontweight='bold')
    
    # 8. Summary Statistics
    ax8 = fig.add_subplot(gs[3, :])
    ax8.axis('off')
    
    summary_text = "COMBINED DATASET SUMMARY:\n\n"
    summary_text += f"Total Observations: {len(combined_df)}\n"
    summary_text += f"Date Range: {combined_df['date'].min().strftime('%Y-%m-%d')} to {combined_df['date'].max().strftime('%Y-%m-%d')}\n"
    summary_text += f"Unique Sections: {combined_df['section'].nunique()}\n"
    summary_text += f"Unique Metrics: {combined_df['metric'].nunique()}\n"
    summary_text += f"MAUI Observation Types: {len(maui_types)}\n\n"
    
    summary_text += "ORIGINAL DATASET:\n"
    summary_text += f"• Observations: {len(original_df)}\n"
    summary_text += f"• Metrics: {original_df['metric'].nunique()}\n"
    summary_text += f"• Sections: {original_df['section'].nunique()}\n"
    summary_text += f"• MAUI Types: {original_df[original_df['metric'].isin(maui_types)].shape[0]}\n\n"
    
    summary_text += "SAMPLE DATASET:\n"
    summary_text += f"• Observations: {len(sample_df)}\n"
    summary_text += f"• Metrics: {sample_df['metric'].nunique()}\n"
    summary_text += f"• Sections: {sample_df['section'].nunique()}\n"
    summary_text += f"• MAUI Types: {sample_df[sample_df['metric'].isin(maui_types)].shape[0]}\n\n"
    
    summary_text += "NEW METRICS ADDED:\n"
    new_metrics = sample_metrics - original_metrics
    for metric in sorted(new_metrics):
        summary_text += f"• {metric}\n"
    
    ax8.text(0.05, 0.95, summary_text, transform=ax8.transAxes, fontsize=9,
             verticalalignment='top', bbox=dict(boxstyle='round', facecolor='lightyellow', alpha=0.8))
    
    # Save the dashboard
    plt.savefig('combined_datasets_dashboard.png', dpi=300, bbox_inches='tight')
    print("Combined Datasets Dashboard saved as 'combined_datasets_dashboard.png'")
    
    # Generate combined report
    generate_combined_report(original_df, sample_df, combined_df, maui_types)
    
    plt.show()

def generate_combined_report(original_df, sample_df, combined_df, maui_types):
    """Generate a detailed report comparing the datasets"""
    
    report = []
    report.append("=" * 80)
    report.append("COMBINED DATASETS ANALYSIS REPORT")
    report.append("=" * 80)
    report.append(f"Report Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    report.append("")
    
    # Overall Summary
    report.append("OVERALL SUMMARY:")
    report.append("-" * 40)
    report.append(f"Original Dataset: {len(original_df)} observations")
    report.append(f"Sample Dataset: {len(sample_df)} observations")
    report.append(f"Combined Dataset: {len(combined_df)} observations")
    report.append(f"Date Range: {combined_df['date'].min().strftime('%Y-%m-%d')} to {combined_df['date'].max().strftime('%Y-%m-%d')}")
    report.append("")
    
    # Metric Analysis
    report.append("METRIC ANALYSIS:")
    report.append("-" * 40)
    original_metrics = set(original_df['metric'].unique())
    sample_metrics = set(sample_df['metric'].unique())
    
    report.append(f"Original Metrics: {len(original_metrics)}")
    report.append(f"Sample Metrics: {len(sample_metrics)}")
    report.append(f"Combined Unique Metrics: {len(original_metrics.union(sample_metrics))}")
    report.append(f"Overlap: {len(original_metrics.intersection(sample_metrics))}")
    report.append("")
    
    # New Metrics Added
    new_metrics = sample_metrics - original_metrics
    report.append("NEW METRICS ADDED BY SAMPLE DATASET:")
    report.append("-" * 40)
    for metric in sorted(new_metrics):
        count = sample_df[sample_df['metric'] == metric].shape[0]
        report.append(f"• {metric}: {count} observations")
    report.append("")
    
    # MAUI Observation Types Analysis
    report.append("MAUI OBSERVATION TYPES ANALYSIS:")
    report.append("-" * 40)
    report.append("Original Dataset:")
    for maui_type in maui_types:
        count = original_df[original_df['metric'] == maui_type].shape[0]
        report.append(f"  • {maui_type}: {count} observations")
    
    report.append("\nSample Dataset:")
    for maui_type in maui_types:
        count = sample_df[sample_df['metric'] == maui_type].shape[0]
        report.append(f"  • {maui_type}: {count} observations")
    
    report.append("\nCombined Dataset:")
    for maui_type in maui_types:
        count = combined_df[combined_df['metric'] == maui_type].shape[0]
        report.append(f"  • {maui_type}: {count} observations")
    report.append("")
    
    # Section Analysis
    report.append("SECTION ANALYSIS:")
    report.append("-" * 40)
    original_sections = set(original_df['section'].unique())
    sample_sections = set(sample_df['section'].unique())
    
    report.append(f"Original Sections: {len(original_sections)}")
    report.append(f"Sample Sections: {len(sample_sections)}")
    report.append(f"Combined Sections: {len(original_sections.union(sample_sections))}")
    report.append("")
    
    # Top Metrics in Combined Dataset
    report.append("TOP 10 METRICS IN COMBINED DATASET:")
    report.append("-" * 40)
    top_metrics = combined_df['metric'].value_counts().head(10)
    for metric, count in top_metrics.items():
        percentage = (count / len(combined_df)) * 100
        report.append(f"• {metric}: {count} ({percentage:.1f}%)")
    report.append("")
    
    # Condition Distribution
    report.append("CONDITION DISTRIBUTION IN COMBINED DATASET:")
    report.append("-" * 40)
    condition_counts = combined_df['condition'].value_counts()
    for condition, count in condition_counts.items():
        percentage = (count / len(combined_df)) * 100
        report.append(f"• {condition.capitalize()}: {count} ({percentage:.1f}%)")
    report.append("")
    
    report.append("=" * 80)
    
    # Save report
    with open('combined_datasets_report.txt', 'w', encoding='utf-8') as f:
        f.write('\n'.join(report))
    
    print('\n'.join(report))
    print("\nCombined report saved as 'combined_datasets_report.txt'")

if __name__ == "__main__":
    combine_datasets() 