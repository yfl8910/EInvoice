# 发票 6250035639：PDF / XML 字段对照

核对日期：2026-10-19。范围：历史 PDF 的 1 页完整内容、同号 XML，以及同目录另外 5 份 XML 的字段结构。其他 5 张没有提供对应 PDF，不能据此认定其内容与历史 PDF 一致。

## 来源

- 历史 PDF：[6250035639.pdf](D:/Mycode/电子发票/6250035639.pdf)
- 同号 XML：[625003563920260924230245.xml](D:/Mycode/电子发票/625003563920260924230245.xml)
- PDF 无内嵌 XML 附件；本次对照使用外部 XML 文件。
- 下文“XML 缺失”表示本次提供的 XML 未发现该信息，不等于该字段在法规或 XRechnung 中一定必填。本次未执行正式合规验证。

## 已确认一致的核心数据

| 字段 | PDF | XML | 结论 |
| --- | --- | --- | --- |
| 发票号码 | 6250035639 | 6250035639 | 一致 |
| 开票日期 | 2026-08-11 | 2026-08-11 | 一致 |
| 到期日期 | 2026-10-10 | 2026-10-10 | 一致 |
| 供应商 | TCL Deutschland GmbH & Co. KG | 同名 | 一致 |
| 买方 | INGRAM MICRO DISTRIBUTION GMBH | 同名 | 一致 |
| 供应商 VAT | DE815701343 | 同值 | 一致，已补展示 |
| 买方 VAT | DE129422178 | 同值 | 一致，已补展示 |
| WEEE | DE 30824838 | PartyLegalEntity/CompanyID = WEEE: DE 30824838 | 值已包含；界面按原始实体标识展示，不将其冒充商业注册号 |
| 送货单 | 3000511739 | DespatchDocumentReference/ID = 3000511739 | 一致 |
| 付款条件 | 60 Days Net | Note = N060 60 Days Net | XML 保留额外代码 N060，展示原文 |
| 币种 | EUR | EUR | 一致 |
| 商品描述 | 50P7K；TCL 2023/50/4K/Y/QLED TV/Black | 50P7K 2023/50/4K/Y/QLED TV/Black | 主要型号和规格相符，原始描述不同 |
| EAN / 标准商品标识 | 5901292525941 | 5901292525941，schemeID = 0160 | 值一致，schemeID 原样保留 |
| 数量 | 188 | 188.000，unitCode = EA | 数值一致，单位保留 |
| 单价 | 216.20 | 216.2 | 数值一致 |
| 净额 | 40645.60 | 40645.60 | 一致 |
| 税率 | 19% | 19 | 一致，新增税额分解展示 |
| 税额 | 7722.66 | 7722.66 | 一致 |
| 含税总额 | 48368.26 | TaxInclusiveAmount = 48368.26 | 一致；含税总额与应付金额分别展示 |
| 应付金额 | 48368.26 | PayableAmount = 48368.26 | 一致 |
| EUR 银行账号 | IBAN DE30 3003 0880 0014 7820 01 | 同值 | 一致；不清洗或改写账号原文 |
| 银行名称 | HSBC CONTINENTAL EUROPE S.A., GERMANY | 同值 | 一致，已补展示 |
| SWIFT | TUBDDEDD | FinancialInstitutionBranch/ID = TUBDDEDD | 一致，按原始银行标识展示 |

## 需要确认的冲突及映射问题

| 优先级 | 字段 | PDF | XML | 处理 |
| --- | --- | --- | --- | --- |
| 高 | 实际交付日期 | 2026.08.11 | Delivery/ActualDeliveryDate = 2026-08-04 | 相差 7 天。联系开票方确认；系统继续显示 XML 原值，不自动覆盖 |
| 高 | 采购订单 / 形式发票引用 | CUSTOMER PO = 44-YWZ69-S；PROFORMA = 9964353062 | BuyerReference = 44-YWZ69-S；OrderReference/ID = 9964353062 | 数字未丢，但字段语义可能错位。OrderReference/ID 的标准语义是买方采购订单引用；不能未经确认把形式发票号当作采购订单号 |
| 中 | 供应商地址拼写 | Bernhard-Wicki-Straße 5；München | Bernhard-Wicki-Strase 5；Munchen | XML 原文已去掉部分德文字符。系统保留 XML 拼写；请上游确认标准地址 |
| 低 | 送货街道格式 | Europaring 20 22 | Europaring 20-22 | 标点格式不同；完整街道、邮编、城市、国家已补展示 |

买方开票地址 HEISENBERGBOGEN, 3 / 85609 Aschheim / Germany 与 XML 对应，XML 国家使用 DE。送货地址为 Europaring 20-22 / 94315 Straubing / DE；两类地址必须分开展示。

## 历史 PDF 中存在，但本次 XML 未包含的信息

| 信息 | 历史 PDF 值 | 本次 XML 状态 / 建议 |
| --- | --- | --- |
| Incoterms / 交货条款 | DDP Dornach (Munich) | 未发现。由上游补充实际贸易条款或可追溯附件 |
| 装载 / 运输方式 | Truck | 未发现 |
| 装货地点 | FIEGE DE Genuine | 未发现 |
| 卸货地点文字 | Straubing Germany | XML 已有 Straubing 送货地址，但没有独立“卸货地点”字段；不要把两个概念强行合并 |
| 客户账号 / 客户编号 | 6002256 | 未发现；注意它不是银行账号 |
| 海关编码 | 8528724000 | 未发现商品分类 / 海关编码字段 |
| 原产国 | China | 未发现商品原产国字段；供应商或送货国家 DE 不能替代原产国 |
| 独立物料号字段 | 50P7K | 型号在商品名称中出现，但没有独立卖方物料标识节点。不要从描述自动推断正式物料编码 |
| 车辆标识 | DEDINE6JY07 | 未发现 |
| 银行地址 | HANSAALLEE 3, 40549 DÜSSELDORF, GERMANY | 未发现 |
| USD 备用账户 | PDF 列出一个 USD 账号 | XML 仅提供 EUR 收款账号；不将备用 USD 账号自动加入 EUR 发票付款指令 |
| 独立收款人 / Beneficiary | TCL Deutschland GmbH & Co. KG | 供应商同名，但没有独立 PayeeParty 或账户名称字段；不把供应商名称当作已明确提供的收款人 |
| 普通合伙人 | TCL DEUTSCHLAND VERWALTUNGS GMBH | 未发现 |
| 注册法院及登记信息 | MÜNCHEN, AMTSGERICHT MÜNCHEN HRA 119811（按 PDF 原文） | 未发现；XML WEEE 标识不能替代商业注册信息 |
| 董事 / 负责人 | ALEX MA | 未发现 |
| 保留所有权 / 交付条款说明 | PDF 提及背面交付条款及所有权保留 | 未发现。PDF 本身仅 1 页，也未包含所指完整背面条款 |

ETA、Forwarder、GBP/PLN 银行账户在 PDF 中本身留空，不能据此认定 XML 遗失了具体值。Logo、版式、页码不属于已核对的业务字段。

## XML 中额外存在的信息

XML 额外提供电子地址及 schemeID、付款方式代码 30、付款引用、供应商联系人/电话/邮箱、发票类型代码 380、XRechnung CustomizationID、Peppol ProfileID、行号、单位及税分类代码。这些信息不能因为历史 PDF 未展示而丢弃。

## 系统补充实现

- 新增 InvoiceDetailProjection，从已保存的原始 XML 叶节点投影出文档、供应商、买方、交付、付款、税额分解及原始总额字段。
- 新增独立 InvoiceAdditionalDetails 组件，由上传预览和已保存发票详情共用；PDF 导出使用相同投影。
- 既有数据库已经保留全部 XML 叶节点，因此旧记录无需重新导入，也无需改数据库表结构。
- 原始 XML、PDF 参考文件及数据库原值均未修改。PDF 独有数据未硬编码进发票，日期冲突未静默“纠正”。

## 字段语义参考

- [Peppol：OrderReference/ID / BT-13](https://docs.peppol.eu/poacc/billing/3.0/syntax/ubl-invoice/cac-OrderReference/cbc-ID/)：买方采购订单引用。
- [Peppol：BuyerReference / BT-10](https://docs.peppol.eu/poacc/billing/3.0/syntax/ubl-invoice/cbc-BuyerReference/)：买方内部路由引用。
- [Peppol：FinancialInstitutionBranch/ID / BT-86](https://docs.peppol.eu/poacc/billing/3.0/syntax/ubl-invoice/cac-PaymentMeans/cac-PayeeFinancialAccount/cac-FinancialInstitutionBranch/cbc-ID/)：支付服务机构标识，例如 BIC 或本国清算代码。

本报告只给出源文件对照和展示完整性判断，不构成税务或法律合规结论。
