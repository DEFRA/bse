SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO

--Create luTseTestingSite table	
CREATE TABLE [dbo].[luTseTestingSite] (
	[Name] [varchar] (50) COLLATE Latin1_General_CI_AS NOT NULL ,
	[Address] [varchar] (255) COLLATE Latin1_General_CI_AS NOT NULL ,
	[CPH] [char] (11) COLLATE Latin1_General_CI_AS NOT NULL ,
	[AHO] [char] (2) COLLATE Latin1_General_CI_AS NOT NULL ,
	CONSTRAINT [FK_luTseTestingSite_AHO] FOREIGN KEY 
	(
		[AHO]
	) REFERENCES [luAHO] (
		[Code]
	)
) ON [PRIMARY]
GO

--Insert data in luTseTestingSite table
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Granox Ltd (PDM)','Desoto Rd, Mersey Multimodal Gateway(3G), Widnes, Cheshire WA8 0PB','06/685/9009','24')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('R Bennett & Sons (Grampound)','Fal Valley Tannery, Grampound, Truro, Cornwall, TR2 4RX','07/049/8000','45')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('F D Statton & Son Ltd','The Slaughter House, Trewassa Moor, Davidstow, PL32 9YF','07/071/0058','45')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Peake (GB) Ltd','Stoneybridge Park, Liskeard, Cornwall, PL14 3NQ','07/156/8007','45')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Duerden Fallen Stock','Lindal Moor Abattoir, Lindal, Ulverston, Cumbria, LA12 0LT','08/674/8000','08')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('F Redfern & Sons Ltd','Chestnut Farm, Flagg, Buxton, Derbyshire, SK17 9QR','09/078/8001','24')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Cremtor','Greycote Lane, Forches Cross, Newton Abbot, TQ12 6PX','10/371/7000','44')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('North East Incineration Services','Tellam''s Yard, Cheriton Bishop, Exeter, Devon, EX6 6HH','10/440/0127','44')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('J L Thomas Ltd (PDM Group)','Spring Gardens, Canal Bank, Exeter, Devon EX2 8DX','10/463/8030','44')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Fromevale Animal Products','The Old Barn, Kingcombe Cross, Rampisham, Dorset DT2 0QD','11/093/8000','42')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('J Warren ABP Ltd','Eden Hall, Hamsterley, Bishop Auckland, County Durham','12/041/9007','01')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('W Martin & Son','Bakers Lane, Black Notley, Braintree, Essex, CM77 8QS','13/295/9006 ','33')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('N J Robinson Farms Ltd','Home Farm, Besford, Worcester WR8 9AP','17/395/8000','27')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Davenport Fallen Stock Ltd','Yew Tree Farm, Fairfield, Bromsgrove, Worcs. B61 9HW','17/439/8002','27')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Bowness','Bonnie Brae, The Rampings, Longdon, Nr Tewkesbury, Gloucs,GL20 6AN','17/472/8001','27')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('John Knights ABP Ltd (PDM)','Canterbury Mills, Pennpot Lane, Waltham, Canterbury Kent CT4 7EL','20/207/0101','35')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Martlands','Bartons Farm, Moss Lane, Burscough, nr Ormskirk Lancs. L40 4AU','21/373/8002','08')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('A Hughes and Son Ltd','Jerusalem Farm, Skellingthorpe, Lincoln, LN6 4RL','24/075/8002','12')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('G W Lord','Westwinds, Walkerith Rd, Morton Gainsborough, Lincs DN21 3DF','24/515/9016','12')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('T J Kent','The Bungalow, Swamp Lane, Great Ellingham, Attleborough Norwich, Norfolk','28/333/9024','17')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Major Farm Services Ltd','Blackstone Farm, Bicester Road, Rr Blackthorn, OX25 1HX','33/272/8001','39')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Chew Valley Hide & Shin Co Ltd','Tinkers Lane, Compton Martin, Nr Bristol BS40 6JN','34/794/8000','28')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('John Pointon & Sons','Felthouse Lane, Cheddleton, Staffordshire ST13 7BT','37/065/8001','24')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Brassington & co','Easing Moor Farm, Thorncliffe, Leek, Staffs, ST13 ','37/075/0050','24')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Rick Strain & Sons Ltd','The Embankment, Power Station Rd., Rugeley, Staffs, WS15 2HS','37/165/9743','24')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Clarkes of Melton Ltd','The Slaughter House, Valley Farm Road, Melton, Woodbridge Suffolk, IP12 1LL','38/318/9016','17')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Charles Diplock','Diplocks, Bishops Lane, Ringmeer, Lewes, E Sussex BN8 5TJ','41/110/8002','35')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Harry Hawkins','Chates Farm, Henfield road, Cowfold, W Sussex, RH13 8DU','42/073/8003','35')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('J G Pears Ltd','Bella Vista Farm, Hartcliffe, Penistone, Nr Sheffield','47/727/0196','07')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Harry Atkinson','Mayfield House, Malton Road, Pickering N. Yorks YO18 8EA','48/467/0380','07')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Green Brothers','Willow Grove, Murton, York, YO19 5XE','48/495/9006','07')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Robinson Mitchel Ltd','Over Raygill, Dumb Toms Lane, Ingleton, Via Carnforth, Lancs LA6 3DS','48/858/9014','07')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Omega Proteins limited','Building 4 Swalesmoor Farm Swalesmoor Rd, Halifax','49/349/8015','07')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Boddy','East Heyhead Farm, Cross Stone Road,Todmorden, West Yorkshire OL14 8RE','49/357/0069','07')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Omega Proteins limited','Erlings Works, Half Acre Road, Thornton, Bradford BD13 3SG','49/528/8010','07')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('P Waddington & Co Ltd','54 Buck Street, Bradford, BD3 9LP','49/528/8023','07')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('WRE Collection Services Ltd','The Kennels, Canal Lane, Brecon LD3 7PL','52/063/8016','56')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Rural Incineration & Disposal Ltd','Pant, Little London, Llandinam, Powys SY17 5AF','52/119/8000','47')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('F M Caine & Sons Ltd','The Factory, Penybont Road, Knighton, Powys LD7 1SD','52/226/8001','56')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Mid Wales Incineration Ltd','Oakfield Nant Glas, Llandrindod Wells, Powys, LD1 6PA','52/260/8004 ','56')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Cymru Lan','Plot 4, Gaerwen Industrial Estate, Gaerwen, Anglesey, LL60 6HR','53/102/8005','47')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Douglas Bros','Factory Yard, Cwmann, Lampeter, SA48 8ES','55/055/8529','57')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Tom Goddard and Sons Ltd','The Causeway Factory, Camrose, Haverfordwest, SA62 6HG','55/531/8434 ','57')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Animal Waste Services Ltd','Tyddn Daniel, Bedwell Road, Marchwell, Wrexham LL13 0TS','56/023/8004','47')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Clutton Agriculture Ltd','Tyddn Daniel, Bedwell Road, Marchwell, Wrexham LL13 0TS','56/023/8004','47')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('North East Incineration Services','Douglasbrae, Keith, Banffshire, AB55 5HT','70/240/8001','65')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Dundas Chemical Company','Mosspark, Dumfries, DG1 4PH','75/300/8009','72')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Grayshill Ltd','Grayshill ltd. Mollins Road, Cumbernauld, G68 9BA ','76/342/8500','78')
INSERT INTO [dbo].[luTseTestingSite] VALUES ('Clifford Casualty Service','Clifford Smithers, 27 St Couans Place, Newton Stewart, DG8 6LY','82/517/8001','72')


--Insert info about luTseTestingSite table in [dbo].[EditableLookup] table
INSERT INTO [dbo].[EditableLookUp] VALUES (27,'luTseTestingSite','TSE Testing Site','GetluTseTestingSite','EditluTseTestingSite','AddluTseTestingSite','DeleteluTseTestingSite')
GO

SET ANSI_PADDING OFF

--add TseTestingSite and SamplingDate columns in [dbo].[CaseWork] table
ALTER TABLE [dbo].[CaseWork]
  ADD 
      [TSETestingSite] [varchar] (50),
      [SamplingDate] datetime
    



