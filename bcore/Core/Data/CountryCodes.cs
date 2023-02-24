﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lnksnk.Core.Data
{
    public class CountryCodes
    {
        private const string countrycodesvls = @"English short name	French short name	Alpha-2 code	Alpha-3 code	Numeric
Afghanistan	Afghanistan (l')	AF	AFG	004
Albania	Albanie (l')	AL	ALB	008
Algeria	Algérie (l')	DZ	DZA	012
American Samoa	Samoa américaines (les)	AS	ASM	016
Andorra	Andorre (l')	AD	AND	020
Angola	Angola (l')	AO	AGO	024
Anguilla	Anguilla	AI	AIA	660
Antarctica	Antarctique (l')	AQ	ATA	010
Antigua and Barbuda	Antigua-et-Barbuda	AG	ATG	028
Argentina	Argentine (l')	AR	ARG	032
Armenia	Arménie (l')	AM	ARM	051
Aruba	Aruba	AW	ABW	533
Australia	Australie (l')	AU	AUS	036
Austria	Autriche (l')	AT	AUT	040
Azerbaijan	Azerbaïdjan (l')	AZ	AZE	031
Bahamas (the)	Bahamas (les)	BS	BHS	044
Bahrain	Bahreïn	BH	BHR	048
Bangladesh	Bangladesh (le)	BD	BGD	050
Barbados	Barbade (la)	BB	BRB	052
Belarus	Bélarus (le)	BY	BLR	112
Belgium	Belgique (la)	BE	BEL	056
Belize	Belize (le)	BZ	BLZ	084
Benin	Bénin (le)	BJ	BEN	204
Bermuda	Bermudes (les)	BM	BMU	060
Bhutan	Bhoutan (le)	BT	BTN	064
Bolivia (Plurinational State of)	Bolivie (État plurinational de)	BO	BOL	068
Bonaire, Sint Eustatius and Saba	Bonaire, Saint-Eustache et Saba	BQ	BES	535
Bosnia and Herzegovina	Bosnie-Herzégovine (la)	BA	BIH	070
Botswana	Botswana (le)	BW	BWA	072
Bouvet Island	Bouvet (l'Île)	BV	BVT	074
Brazil	Brésil (le)	BR	BRA	076
British Indian Ocean Territory (the)	Indien (le Territoire britannique de l'océan)	IO	IOT	086
Brunei Darussalam	Brunéi Darussalam (le)	BN	BRN	096
Bulgaria	Bulgarie (la)	BG	BGR	100
Burkina Faso	Burkina Faso (le)	BF	BFA	854
Burundi	Burundi (le)	BI	BDI	108
Cabo Verde	Cabo Verde	CV	CPV	132
Cambodia	Cambodge (le)	KH	KHM	116
Cameroon	Cameroun (le)	CM	CMR	120
Canada	Canada (le)	CA	CAN	124
Cayman Islands (the)	Caïmans (les Îles)	KY	CYM	136
Central African Republic (the)	République centrafricaine (la)	CF	CAF	140
Chad	Tchad (le)	TD	TCD	148
Chile	Chili (le)	CL	CHL	152
China	Chine (la)	CN	CHN	156
Christmas Island	Christmas (l'Île)	CX	CXR	162
Cocos (Keeling) Islands (the)	Cocos (les Îles)/ Keeling (les Îles)	CC	CCK	166
Colombia	Colombie (la)	CO	COL	170
Comoros (the)	Comores (les)	KM	COM	174
Congo (the Democratic Republic of the)	Congo (la République démocratique du)	CD	COD	180
Congo (the)	Congo (le)	CG	COG	178
Cook Islands (the)	Cook (les Îles)	CK	COK	184
Costa Rica	Costa Rica (le)	CR	CRI	188
Croatia	Croatie (la)	HR	HRV	191
Cuba	Cuba	CU	CUB	192
Curaçao	Curaçao	CW	CUW	531
Cyprus	Chypre	CY	CYP	196
Czechia	Tchéquie (la)	CZ	CZE	203
Côte d'Ivoire	Côte d'Ivoire (la)	CI	CIV	384
Denmark	Danemark (le)	DK	DNK	208
Djibouti	Djibouti	DJ	DJI	262
Dominica	Dominique (la)	DM	DMA	212
Dominican Republic (the)	dominicaine (la République)	DO	DOM	214
Ecuador	Équateur (l')	EC	ECU	218
Egypt	Égypte (l')	EG	EGY	818
El Salvador	El Salvador	SV	SLV	222
Equatorial Guinea	Guinée équatoriale (la)	GQ	GNQ	226
Eritrea	Érythrée (l')	ER	ERI	232
Estonia	Estonie (l')	EE	EST	233
Eswatini	Eswatini (l')	SZ	SWZ	748
Ethiopia	Éthiopie (l')	ET	ETH	231
Falkland Islands (the) [Malvinas]	Falkland (les Îles)/Malouines (les Îles)	FK	FLK	238
Faroe Islands (the)	Féroé (les Îles)	FO	FRO	234
Fiji	Fidji (les)	FJ	FJI	242
Finland	Finlande (la)	FI	FIN	246
France	France (la)	FR	FRA	250
French Guiana	Guyane française (la )	GF	GUF	254
French Polynesia	Polynésie française (la)	PF	PYF	258
French Southern Territories (the)	Terres australes françaises (les)	TF	ATF	260
Gabon	Gabon (le)	GA	GAB	266
Gambia (the)	Gambie (la)	GM	GMB	270
Georgia	Géorgie (la)	GE	GEO	268
Germany	Allemagne (l')	DE	DEU	276
Ghana	Ghana (le)	GH	GHA	288
Gibraltar	Gibraltar	GI	GIB	292
Greece	Grèce (la)	GR	GRC	300
Greenland	Groenland (le)	GL	GRL	304
Grenada	Grenade (la)	GD	GRD	308
Guadeloupe	Guadeloupe (la)	GP	GLP	312
Guam	Guam	GU	GUM	316
Guatemala	Guatemala (le)	GT	GTM	320
Guernsey	Guernesey	GG	GGY	831
Guinea	Guinée (la)	GN	GIN	324
Guinea-Bissau	Guinée-Bissau (la)	GW	GNB	624
Guyana	Guyana (le)	GY	GUY	328
Haiti	Haïti	HT	HTI	332
Heard Island and McDonald Islands	Heard-et-Îles MacDonald (l'Île)	HM	HMD	334
Holy See (the)	Saint-Siège (le)	VA	VAT	336
Honduras	Honduras (le)	HN	HND	340
Hong Kong	Hong Kong	HK	HKG	344
Hungary	Hongrie (la)	HU	HUN	348
Iceland	Islande (l')	IS	ISL	352
India	Inde (l')	IN	IND	356
Indonesia	Indonésie (l')	ID	IDN	360
Iran (Islamic Republic of)	Iran (République Islamique d')	IR	IRN	364
Iraq	Iraq (l')	IQ	IRQ	368
Ireland	Irlande (l')	IE	IRL	372
Isle of Man	Île de Man	IM	IMN	833
Israel	Israël	IL	ISR	376
Italy	Italie (l')	IT	ITA	380
Jamaica	Jamaïque (la)	JM	JAM	388
Japan	Japon (le)	JP	JPN	392
Jersey	Jersey	JE	JEY	832
Jordan	Jordanie (la)	JO	JOR	400
Kazakhstan	Kazakhstan (le)	KZ	KAZ	398
Kenya	Kenya (le)	KE	KEN	404
Kiribati	Kiribati	KI	KIR	296
Korea (the Democratic People's Republic of)	Corée (la République populaire démocratique de)	KP	PRK	408
Korea (the Republic of)	Corée (la République de)	KR	KOR	410
Kuwait	Koweït (le)	KW	KWT	414
Kyrgyzstan	Kirghizistan (le)	KG	KGZ	417
Lao People's Democratic Republic (the)	Lao (la République démocratique populaire)	LA	LAO	418
Latvia	Lettonie (la)	LV	LVA	428
Lebanon	Liban (le)	LB	LBN	422
Lesotho	Lesotho (le)	LS	LSO	426
Liberia	Libéria (le)	LR	LBR	430
Libya	Libye (la)	LY	LBY	434
Liechtenstein	Liechtenstein (le)	LI	LIE	438
Lithuania	Lituanie (la)	LT	LTU	440
Luxembourg	Luxembourg (le)	LU	LUX	442
Macao	Macao	MO	MAC	446
Madagascar	Madagascar	MG	MDG	450
Malawi	Malawi (le)	MW	MWI	454
Malaysia	Malaisie (la)	MY	MYS	458
Maldives	Maldives (les)	MV	MDV	462
Mali	Mali (le)	ML	MLI	466
Malta	Malte	MT	MLT	470
Marshall Islands (the)	Marshall (les Îles)	MH	MHL	584
Martinique	Martinique (la)	MQ	MTQ	474
Mauritania	Mauritanie (la)	MR	MRT	478
Mauritius	Maurice	MU	MUS	480
Mayotte	Mayotte	YT	MYT	175
Mexico	Mexique (le)	MX	MEX	484
Micronesia (Federated States of)	Micronésie (États fédérés de)	FM	FSM	583
Moldova (the Republic of)	Moldova (la République de)	MD	MDA	498
Monaco	Monaco	MC	MCO	492
Mongolia	Mongolie (la)	MN	MNG	496
Montenegro	Monténégro (le)	ME	MNE	499
Montserrat	Montserrat	MS	MSR	500
Morocco	Maroc (le)	MA	MAR	504
Mozambique	Mozambique (le)	MZ	MOZ	508
Myanmar	Myanmar (le)	MM	MMR	104
Namibia	Namibie (la)	NA	NAM	516
Nauru	Nauru	NR	NRU	520
Nepal	Népal (le)	NP	NPL	524
Netherlands (the)	Pays-Bas (les)	NL	NLD	528
New Caledonia	Nouvelle-Calédonie (la)	NC	NCL	540
New Zealand	Nouvelle-Zélande (la)	NZ	NZL	554
Nicaragua	Nicaragua (le)	NI	NIC	558
Niger (the)	Niger (le)	NE	NER	562
Nigeria	Nigéria (le)	NG	NGA	566
Niue	Niue	NU	NIU	570
Norfolk Island	Norfolk (l'Île)	NF	NFK	574
North Macedonia	Macédoine du Nord (la)	MK	MKD	807
Northern Mariana Islands (the)	Mariannes du Nord (les Îles)	MP	MNP	580
Norway	Norvège (la)	NO	NOR	578
Oman	Oman	OM	OMN	512
Pakistan	Pakistan (le)	PK	PAK	586
Palau	Palaos (les)	PW	PLW	585
Palestine, State of	Palestine, État de	PS	PSE	275
Panama	Panama (le)	PA	PAN	591
Papua New Guinea	Papouasie-Nouvelle-Guinée (la)	PG	PNG	598
Paraguay	Paraguay (le)	PY	PRY	600
Peru	Pérou (le)	PE	PER	604
Philippines (the)	Philippines (les)	PH	PHL	608
Pitcairn	Pitcairn	PN	PCN	612
Poland	Pologne (la)	PL	POL	616
Portugal	Portugal (le)	PT	PRT	620
Puerto Rico	Porto Rico	PR	PRI	630
Qatar	Qatar (le)	QA	QAT	634
Romania	Roumanie (la)	RO	ROU	642
Russian Federation (the)	Russie (la Fédération de)	RU	RUS	643
Rwanda	Rwanda (le)	RW	RWA	646
Réunion	Réunion (La)	RE	REU	638
Saint Barthélemy	Saint-Barthélemy	BL	BLM	652
Saint Helena, Ascension and Tristan da Cunha	Sainte-Hélène, Ascension et Tristan da Cunha	SH	SHN	654
Saint Kitts and Nevis	Saint-Kitts-et-Nevis	KN	KNA	659
Saint Lucia	Sainte-Lucie	LC	LCA	662
Saint Martin (French part)	Saint-Martin (partie française)	MF	MAF	663
Saint Pierre and Miquelon	Saint-Pierre-et-Miquelon	PM	SPM	666
Saint Vincent and the Grenadines	Saint-Vincent-et-les Grenadines	VC	VCT	670
Samoa	Samoa (le)	WS	WSM	882
San Marino	Saint-Marin	SM	SMR	674
Sao Tome and Principe	Sao Tomé-et-Principe	ST	STP	678
Saudi Arabia	Arabie saoudite (l')	SA	SAU	682
Senegal	Sénégal (le)	SN	SEN	686
Serbia	Serbie (la)	RS	SRB	688
Seychelles	Seychelles (les)	SC	SYC	690
Sierra Leone	Sierra Leone (la)	SL	SLE	694
Singapore	Singapour	SG	SGP	702
Sint Maarten (Dutch part)	Saint-Martin (partie néerlandaise)	SX	SXM	534
Slovakia	Slovaquie (la)	SK	SVK	703
Slovenia	Slovénie (la)	SI	SVN	705
Solomon Islands	Salomon (les Îles)	SB	SLB	090
Somalia	Somalie (la)	SO	SOM	706
South Africa	Afrique du Sud (l')	ZA	ZAF	710
South Georgia and the South Sandwich Islands	Géorgie du Sud-et-les Îles Sandwich du Sud (la)	GS	SGS	239
South Sudan	Soudan du Sud (le)	SS	SSD	728
Spain	Espagne (l')	ES	ESP	724
Sri Lanka	Sri Lanka	LK	LKA	144
Sudan (the)	Soudan (le)	SD	SDN	729
Suriname	Suriname (le)	SR	SUR	740
Svalbard and Jan Mayen	Svalbard et l'Île Jan Mayen (le)	SJ	SJM	744
Sweden	Suède (la)	SE	SWE	752
Switzerland	Suisse (la)	CH	CHE	756
Syrian Arab Republic (the)	République arabe syrienne (la)	SY	SYR	760
Taiwan (Province of China)	Taïwan (Province de Chine)	TW	TWN	158
Tajikistan	Tadjikistan (le)	TJ	TJK	762
Tanzania, the United Republic of	Tanzanie (la République-Unie de)	TZ	TZA	834
Thailand	Thaïlande (la)	TH	THA	764
Timor-Leste	Timor-Leste (le)	TL	TLS	626
Togo	Togo (le)	TG	TGO	768
Tokelau	Tokelau (les)	TK	TKL	772
Tonga	Tonga (les)	TO	TON	776
Trinidad and Tobago	Trinité-et-Tobago (la)	TT	TTO	780
Tunisia	Tunisie (la)	TN	TUN	788
Turkey	Turquie (la)	TR	TUR	792
Turkmenistan	Turkménistan (le)	TM	TKM	795
Turks and Caicos Islands (the)	Turks-et-Caïcos (les Îles)	TC	TCA	796
Tuvalu	Tuvalu (les)	TV	TUV	798
Uganda	Ouganda (l')	UG	UGA	800
Ukraine	Ukraine (l')	UA	UKR	804
United Arab Emirates (the)	Émirats arabes unis (les)	AE	ARE	784
United Kingdom of Great Britain and Northern Ireland (the)	Royaume-Uni de Grande-Bretagne et d'Irlande du Nord (le)	GB	GBR	826
United States Minor Outlying Islands (the)	Îles mineures éloignées des États-Unis (les)	UM	UMI	581
United States of America (the)	États-Unis d'Amérique (les)	US	USA	840
Uruguay	Uruguay (l')	UY	URY	858
Uzbekistan	Ouzbékistan (l')	UZ	UZB	860
Vanuatu	Vanuatu (le)	VU	VUT	548
Venezuela (Bolivarian Republic of)	Venezuela (République bolivarienne du)	VE	VEN	862
Viet Nam	Viet Nam (le)	VN	VNM	704
Virgin Islands (British)	Vierges britanniques (les Îles)	VG	VGB	092
Virgin Islands (U.S.)	Vierges des États-Unis (les Îles)	VI	VIR	850
Wallis and Futuna	Wallis-et-Futuna	WF	WLF	876
Western Sahara*	Sahara occidental (le)*	EH	ESH	732
Yemen	Yémen (le)	YE	YEM	887
Zambia	Zambie (la)	ZM	ZMB	894
Zimbabwe	Zimbabwe (le)	ZW	ZWE	716
Åland Islands	Åland(les Îles)	AX	ALA	248
";

        public static StringReader ContryCodesStream() {
            return new StringReader(countrycodesvls);
        }
    }
}