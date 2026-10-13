"use client";

import { useEffect, useRef } from "react";
import type { DashboardSummary } from "@/schemas/dashboard";
import { formatMonth, formatMoney } from "@/utils/finance";

export function DashboardCharts({ data }: { data: DashboardSummary }) {
  const history = useRef<HTMLDivElement>(null);
  const categories = useRef<HTMLDivElement>(null);
  useEffect(() => {
    let stopped = false;
    const cleanup: (() => void)[] = [];
    async function draw() {
      const [echarts, charts, components, renderers] = await Promise.all([
        import("echarts/core"), import("echarts/charts"), import("echarts/components"), import("echarts/renderers"),
      ]);
      if (stopped || !history.current || !categories.current) return;
      echarts.use([charts.BarChart, charts.PieChart, components.GridComponent, components.LegendComponent, components.TooltipComponent, renderers.SVGRenderer]);
      const cashFlow = echarts.init(history.current, null, { renderer: "svg" });
      const categoryChart = echarts.init(categories.current, null, { renderer: "svg" });
      cashFlow.setOption({
        color: ["#176b49", "#c66a49"], animation: false,
        tooltip: { trigger: "axis", renderMode: "richText", valueFormatter: (value: number) => formatMoney(value) },
        legend: { bottom: 0 }, grid: { left: 70, right: 16, top: 24, bottom: 65 },
        xAxis: { type: "category", data: data.monthlyHistory.map(item => formatMonth(item.month)), axisLabel: { rotate: 35 } },
        yAxis: { type: "value", axisLabel: { formatter: (value: number) => new Intl.NumberFormat("pt-BR", { notation: "compact" }).format(value) } },
        series: [
          { name: "Receitas", type: "bar", data: data.monthlyHistory.map(item => item.income), barMaxWidth: 20 },
          { name: "Despesas", type: "bar", data: data.monthlyHistory.map(item => item.expenses), barMaxWidth: 20 },
        ],
      });
      categoryChart.setOption({
        color: ["#176b49", "#c66a49", "#82a68b", "#d7ad66", "#6c8fa1"], animation: false,
        tooltip: { trigger: "item", renderMode: "richText", valueFormatter: (value: number) => formatMoney(value) },
        series: [{ name: "Despesas", type: "pie", radius: ["40%", "70%"], label: { show: false },
          data: data.expensesByCategory.map(item => ({ name: item.name, value: item.total })) }],
      });
      const observer = new ResizeObserver(() => { cashFlow.resize(); categoryChart.resize(); });
      observer.observe(history.current); observer.observe(categories.current);
      cleanup.push(() => { observer.disconnect(); cashFlow.dispose(); categoryChart.dispose(); });
    }
    void draw().catch(() => { /* Numeric tables below remain available if chart loading fails. */ });
    return () => { stopped = true; cleanup.forEach(dispose => dispose()); };
  }, [data]);
  return <section className="dashboard-charts">
    <article className="panel dashboard-panel"><h2>Receitas e despesas</h2><p>Últimos 12 meses · mês atual até data selecionada</p>
      <div ref={history} className="finance-chart" aria-hidden="true" />
      <details><summary>Ver histórico em tabela</summary><div className="table-scroll"><table><caption className="sr-only">Histórico mensal</caption><thead><tr><th>Mês</th><th>Receitas</th><th>Despesas</th></tr></thead><tbody>{data.monthlyHistory.map(item => <tr key={item.month}><th scope="row">{formatMonth(item.month)}</th><td>{formatMoney(item.income)}</td><td>{formatMoney(item.expenses)}</td></tr>)}</tbody></table></div></details>
    </article>
    <article className="panel dashboard-panel"><h2>Despesas por categoria</h2><p>Lançamentos do mês até data selecionada</p>
      <div ref={categories} className="finance-chart" aria-hidden="true" />
      {data.expensesByCategory.length === 0 ? <p>Nenhuma despesa registrada neste mês.</p> : <ul className="category-totals">{data.expensesByCategory.map(item => <li key={item.categoryId ?? "uncategorized"}><span>{item.name}</span><strong>{formatMoney(item.total)}</strong></li>)}</ul>}
    </article>
  </section>;
}
