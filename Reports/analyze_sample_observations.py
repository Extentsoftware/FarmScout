import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
from datetime import datetime
import numpy as np

def analyze_sample_observations():
    """Analyze the generated sample observations and create a comprehensive dashboard"""
    
    # Read the sample observations
    df = pd.read_csv('sample_observations.csv')
    
    # Parse dates
    df['date'] = pd.to_datetime(df['date'], format='%d/%m/%Y')
    
    # Create the dashboard
    fig = plt.figure(figsize=(20, 16))
    
    # Set up the grid
    gs = fig.add_gridspec(4, 3, hspace=0.4, wspace=0.3)
    
    # 1. Overall Distribution by Condition
    ax1 = fig.add_subplot(gs[0, 0])
    condition_counts = df['condition'].value_counts()
    colors = ['green', 'orange', 'red']
    wedges, texts, autotexts = ax1.pie(condition_counts.values, labels=condition_counts.index, 
                                       autopct='%1.1f%%', colors=colors, startangle=90)
    ax1.set_title('Overall Condition Distribution', fontsize=12, fontweight='bold')
    
    # 2. Section Activity
    ax2 = fig.add_subplot(gs[0, 1])
    section_counts = df['section'].value_counts().sort_values(ascending=True)
    bars = ax2.barh(range(len(section_counts)), section_counts.values, color='lightblue')
    ax2.set_yticks(range(len(section_counts)))
    ax2.set_yticklabels(section_counts.index, fontsize=9)
    ax2.set_xlabel('Number of Observations')
    ax2.set_title('Section Activity', fontsize=12, fontweight='bold')
    
    # 3. MAUI Observation Types vs Traditional Metrics
    ax3 = fig.add_subplot(gs[0, 2])
    
    # Define MAUI types
    maui_types = ['Disease', 'Dead Plant', 'Pest', 'Damage', 'Growth', 'Harvest', 'Weather', 'Soil', 'Soil Moisture']
    
    # Count MAUI vs traditional
    maui_count = df[df['metric'].isin(maui_types)].shape[0]
    traditional_count = df[~df['metric'].isin(maui_types)].shape[0]
    
    maui_data = [maui_count, traditional_count]
    maui_labels = ['MAUI Types', 'Traditional Metrics']
    colors_maui = ['#FF6B6B', '#4ECDC4']
    
    wedges, texts, autotexts = ax3.pie(maui_data, labels=maui_labels, autopct='%1.1f%%', 
                                       colors=colors_maui, startangle=90)
    ax3.set_title('MAUI vs Traditional Metrics', fontsize=12, fontweight='bold')
    
    # 4. Top 10 Metrics Distribution
    ax4 = fig.add_subplot(gs[1, 0])
    top_metrics = df['metric'].value_counts().head(10)
    bars = ax4.barh(range(len(top_metrics)), top_metrics.values, color='lightcoral')
    ax4.set_yticks(range(len(top_metrics)))
    ax4.set_yticklabels(top_metrics.index, fontsize=8)
    ax4.set_xlabel('Count')
    ax4.set_title('Top 10 Metrics', fontsize=12, fontweight='bold')
    
    # 5. Condition by Section Heatmap
    ax5 = fig.add_subplot(gs[1, 1:])
    condition_section = pd.crosstab(df['section'], df['condition'])
    sns.heatmap(condition_section, annot=True, fmt='d', cmap='YlOrRd', ax=ax5)
    ax5.set_title('Condition by Section Heatmap', fontsize=12, fontweight='bold')
    ax5.set_xlabel('Condition')
    ax5.set_ylabel('Section')
    
    # 6. MAUI Observation Types Performance
    ax6 = fig.add_subplot(gs[2, :2])
    
    # Filter for MAUI types only
    maui_df = df[df['metric'].isin(maui_types)]
    maui_condition = pd.crosstab(maui_df['metric'], maui_df['condition'])
    
    # Calculate performance score (pass=3, partial=2, fail=1)
    maui_condition['score'] = (maui_condition['pass'] * 3 + maui_condition['partial'] * 2 + maui_condition['fail'] * 1) / maui_condition.sum(axis=1)
    
    bars = ax6.barh(range(len(maui_condition)), maui_condition['score'], color='lightgreen')
    ax6.set_yticks(range(len(maui_condition)))
    ax6.set_yticklabels(maui_condition.index, fontsize=10)
    ax6.set_xlabel('Performance Score (1=Fail, 2=Partial, 3=Pass)')
    ax6.set_title('MAUI Observation Types Performance', fontsize=12, fontweight='bold')
    ax6.set_xlim(0, 3)
    ax6.grid(True, alpha=0.3)
    
    # Add value labels
    for i, bar in enumerate(bars):
        width = bar.get_width()
        ax6.text(width + 0.05, bar.get_y() + bar.get_height()/2, 
                f'{width:.2f}', ha='left', va='center', fontweight='bold')
    
    # 7. Timeline Analysis
    ax7 = fig.add_subplot(gs[2, 2])
    
    # Daily observation count
    daily_counts = df.groupby('date').size()
    ax7.plot(daily_counts.index, daily_counts.values, marker='o', linewidth=1, markersize=3)
    ax7.set_title('Daily Observation Count', fontsize=12, fontweight='bold')
    ax7.set_xlabel('Date')
    ax7.set_ylabel('Observations')
    plt.setp(ax7.xaxis.get_majorticklabels(), rotation=45)
    ax7.grid(True, alpha=0.3)
    
    # 8. Detailed Summary
    ax8 = fig.add_subplot(gs[3, :])
    ax8.axis('off')
    
    # Generate summary text
    summary_text = "SAMPLE OBSERVATIONS ANALYSIS SUMMARY:\n\n"
    summary_text += f"Total Observations: {len(df)}\n"
    summary_text += f"Date Range: {df['date'].min().strftime('%Y-%m-%d')} to {df['date'].max().strftime('%Y-%m-%d')}\n"
    summary_text += f"Unique Sections: {df['section'].nunique()}\n"
    summary_text += f"Unique Metrics: {df['metric'].nunique()}\n"
    summary_text += f"MAUI Observation Types: {len(maui_types)} (all included)\n\n"
    
    summary_text += "CONDITION DISTRIBUTION:\n"
    for condition, count in condition_counts.items():
        percentage = (count / len(df)) * 100
        summary_text += f"• {condition.capitalize()}: {count} ({percentage:.1f}%)\n"
    
    summary_text += "\nMAUI OBSERVATION TYPES COVERAGE:\n"
    for maui_type in maui_types:
        count = df[df['metric'] == maui_type].shape[0]
        summary_text += f"• {maui_type}: {count} observations\n"
    
    summary_text += "\nTOP 5 SECTIONS BY ACTIVITY:\n"
    top_sections = section_counts.head(5)
    for section, count in top_sections.items():
        summary_text += f"• {section}: {count} observations\n"
    
    ax8.text(0.05, 0.95, summary_text, transform=ax8.transAxes, fontsize=10,
             verticalalignment='top', bbox=dict(boxstyle='round', facecolor='lightyellow', alpha=0.8))
    
    # Save the dashboard
    plt.savefig('sample_observations_dashboard.png', dpi=300, bbox_inches='tight')
    print("Sample Observations Dashboard saved as 'sample_observations_dashboard.png'")
    
    # Generate text report
    generate_sample_text_report(df, maui_types)
    
    plt.show()

def generate_sample_text_report(df, maui_types):
    """Generate a detailed text report for the sample observations"""
    
    report = []
    report.append("=" * 80)
    report.append("SAMPLE OBSERVATIONS ANALYSIS REPORT")
    report.append("=" * 80)
    report.append(f"Report Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    report.append(f"Data Range: {df['date'].min().strftime('%Y-%m-%d')} to {df['date'].max().strftime('%Y-%m-%d')}")
    report.append(f"Total Observations: {len(df)}")
    report.append("")
    
    # Summary Statistics
    report.append("SUMMARY STATISTICS:")
    report.append("-" * 40)
    report.append(f"Total Observations: {len(df)}")
    report.append(f"Unique Sections: {df['section'].nunique()}")
    report.append(f"Unique Metrics: {df['metric'].nunique()}")
    report.append(f"MAUI Observation Types: {len(maui_types)}")
    report.append("")
    
    # Condition Distribution
    report.append("CONDITION DISTRIBUTION:")
    report.append("-" * 40)
    condition_counts = df['condition'].value_counts()
    for condition, count in condition_counts.items():
        percentage = (count / len(df)) * 100
        report.append(f"• {condition.capitalize()}: {count} ({percentage:.1f}%)")
    report.append("")
    
    # Section Distribution
    report.append("SECTION DISTRIBUTION:")
    report.append("-" * 40)
    section_counts = df['section'].value_counts().sort_values(ascending=False)
    for section, count in section_counts.items():
        percentage = (count / len(df)) * 100
        report.append(f"• {section}: {count} ({percentage:.1f}%)")
    report.append("")
    
    # MAUI Observation Types Analysis
    report.append("MAUI OBSERVATION TYPES ANALYSIS:")
    report.append("-" * 40)
    for maui_type in maui_types:
        type_data = df[df['metric'] == maui_type]
        if not type_data.empty:
            count = len(type_data)
            condition_dist = type_data['condition'].value_counts()
            report.append(f"\n{maui_type}:")
            report.append(f"  Total: {count} observations")
            for condition, c_count in condition_dist.items():
                c_percentage = (c_count / count) * 100
                report.append(f"  • {condition.capitalize()}: {c_count} ({c_percentage:.1f}%)")
        else:
            report.append(f"\n{maui_type}: No observations")
    report.append("")
    
    # Top Traditional Metrics
    report.append("TOP TRADITIONAL METRICS:")
    report.append("-" * 40)
    traditional_metrics = df[~df['metric'].isin(maui_types)]['metric'].value_counts().head(10)
    for metric, count in traditional_metrics.items():
        percentage = (count / len(df)) * 100
        report.append(f"• {metric}: {count} ({percentage:.1f}%)")
    report.append("")
    
    # Sample Observations
    report.append("SAMPLE OBSERVATIONS (First 20):")
    report.append("-" * 40)
    for i, row in df.head(20).iterrows():
        report.append(f"{i+1:3d}. {row['date'].strftime('%Y-%m-%d')} | {row['section']} | {row['metric']} | {row['condition']} | {row['notes']}")
    
    report.append("")
    report.append("=" * 80)
    
    # Save text report
    with open('sample_observations_report.txt', 'w', encoding='utf-8') as f:
        f.write('\n'.join(report))
    
    # Print report to console
    print('\n'.join(report))
    print("\nText report saved as 'sample_observations_report.txt'")

if __name__ == "__main__":
    analyze_sample_observations() 